using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Terraria.ModLoader.Setup;

internal abstract class SetupOperation(ITaskInterface taskInterface)
{
	protected delegate void UpdateStatus(string status);
	
	protected delegate void Worker(UpdateStatus updateStatus);
	
	protected class WorkItem(string status, Worker worker)
	{
		public string Status { get; } = status;
		
		public Worker Worker { get; } = worker;
		
		public WorkItem(string status, Action action) : this(status, _ => action()) { }
	}
	
	protected void ExecuteParallel(List<WorkItem> items, bool resetProgress = true, int maxDegree = 0)
	{
		try
		{
			if (resetProgress)
			{
				TaskInterface.SetMaxProgress(items.Count);
				Progress = 0;
			}
			
			var working = new List<StrongBox<string>>();
			
			void updateStatus()
			{
				TaskInterface.SetStatus(string.Join("\r\n", working.Select(r => r.Value)));
			}
			
			Parallel.ForEach(
				Partitioner.Create(items, EnumerablePartitionerOptions.NoBuffering),
				new ParallelOptions { MaxDegreeOfParallelism = maxDegree > 0 ? maxDegree : Environment.ProcessorCount, },
				item =>
				{
					TaskInterface.CancellationToken.ThrowIfCancellationRequested();
					var status = new StrongBox<string>(item.Status);
					lock (working)
					{
						working.Add(status);
						updateStatus();
					}
					
					item.Worker(setStatus);
					
					lock (working)
					{
						working.Remove(status);
						TaskInterface.SetProgress(++Progress);
						updateStatus();
					}
					
					return;
					
					void setStatus(string s)
					{
						lock (working)
						{
							status.Value = s;
							updateStatus();
						}
					}
				}
			);
		}
		catch (AggregateException ex)
		{
			var actual = ex.Flatten().InnerExceptions.Where(e => e is not OperationCanceledException).ToList();
			if (actual.Count == 0)
			{
				throw new OperationCanceledException();
			}
			
			throw new AggregateException(actual);
		}
	}
	
	protected static string PreparePath(string path)
	{
		return path.Replace('/', Path.DirectorySeparatorChar);
	}
	
	public static string RelPath(string basePath, string path)
	{
		if (path.Last() == Path.DirectorySeparatorChar)
		{
			path = path[..^1];
		}
		
		if (basePath.Last() != Path.DirectorySeparatorChar)
		{
			basePath += Path.DirectorySeparatorChar;
		}
		
		if (path + Path.DirectorySeparatorChar == basePath)
		{
			return "";
		}
		
		if (!path.StartsWith(basePath))
		{
			path = Path.GetFullPath(path);
			basePath = Path.GetFullPath(basePath);
		}
		
		if (!path.StartsWith(basePath))
		{
			throw new ArgumentException("Path \"" + path + "\" is not relative to \"" + basePath + "\"");
		}
		
		return path[basePath.Length..];
	}
	
	public static void CreateDirectory(string dir)
	{
		if (dir is not null && !Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
	}
	
	public static void CreateParentDirectory(string path)
	{
		CreateDirectory(Path.GetDirectoryName(path));
	}
	
	public static void DeleteFile(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}
		
		File.SetAttributes(path, FileAttributes.Normal);
		File.Delete(path);
	}
	
	protected static void Copy(string from, string to)
	{
		if (from is null || to is null)
		{
			throw new InvalidOperationException($"Attempted to copy to or from a null directory ({from} -> {to})");
		}
		
		CreateParentDirectory(to);
		
		if (File.Exists(to))
		{
			File.SetAttributes(to, FileAttributes.Normal);
		}
		
		File.Copy(from, to, true);
	}
	
	protected static IEnumerable<(string file, string relPath)> EnumerateFiles(string dir)
	{
		return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
			.Select(path => (file: path, relPath: RelPath(dir, path)));
	}
	
	public static void DeleteAllFiles(string dir)
	{
		foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
		{
			File.SetAttributes(file, FileAttributes.Normal);
			File.Delete(file);
		}
	}
	
	protected static bool DeleteEmptyDirs(string dir)
	{
		return !Directory.Exists(dir) || DeleteEmptyDirsRecursion(dir);
	}
	
	private static bool DeleteEmptyDirsRecursion(string dir)
	{
		var allEmpty = Directory.EnumerateDirectories(dir).Aggregate(true, (current, subDir) => current & DeleteEmptyDirsRecursion(subDir));
		
		if (!allEmpty || Directory.EnumerateFiles(dir).Any())
		{
			return false;
		}
		
		Directory.Delete(dir);
		
		return true;
	}
	
	protected ITaskInterface TaskInterface { get; } = taskInterface;
	
	protected int Progress { get; set; }
	
	/// <summary>
	/// Run the task, any exceptions thrown will be written to a log file and update the status label with the exception message
	/// </summary>
	public abstract void Run();
	
	/// <summary>
	/// Display a configuration dialog. Return false if the operation should be cancelled.
	/// </summary>
	public virtual bool ConfigurationDialog()
	{
		return true;
	}
	
	/// <summary>
	/// Display a startup warning dialog
	/// </summary>
	/// <returns>true if the task should continue</returns>
	public virtual bool StartupWarning()
	{
		return true;
	}
	
	/// <summary>
	/// Will prevent successive tasks from executing and cause FinishedDialog to be called
	/// </summary>
	/// <returns></returns>
	public virtual bool Failed()
	{
		return false;
	}
	
	/// <summary>
	/// Will cause FinishedDialog to be called if warnings are not supressed
	/// </summary>
	/// <returns></returns>
	public virtual bool Warnings()
	{
		return false;
	}
	
	/// <summary>
	/// Called to display a finished dialog if Failures() || warnings are not supressed and Warnings()
	/// </summary>
	public virtual void FinishedDialog() { }
}
