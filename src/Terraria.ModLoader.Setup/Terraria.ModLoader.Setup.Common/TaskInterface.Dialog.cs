namespace Terraria.ModLoader.Setup.Common;

public enum SetupDialogResult
{
	None = 0,
	Ok = 1,
	Cancel = 2,
	Abort = 3,
	Retry = 4,
	Ignore = 5,
	Yes = 6,
	No = 7,
	
	/* 8
	 * 9 */
	
	TryAgain = 10,
	Continue = 11,
}

public enum SetupMessageBoxButtons
{
	Ok = 0,
	OkCancel = 1,
	AbortRetryIgnore = 2,
	YesNoCancel = 3,
	YesNo = 4,
	RetryCancel = 5,
	CancelTryContinue = 6,
}

public enum SetupMessageBoxIcon
{
	None = 0,
	
	Error = 16,
	Stop = Error,
	Hand = Error,
	
	Question = 32,
	
	Exclamation = 48,
	Warning = Exclamation,
	
	Asterisk = 64,
	Information = Asterisk,
}

public readonly record struct OpenFileDialogParameters(string FileName, string InitialDirectory, string Filter, string Title);

public interface IDialogTaskInterface : ITaskInterface
{
	SetupDialogResult ShowDialog(string title, string message, SetupMessageBoxButtons buttons, SetupMessageBoxIcon icon);
	
	SetupDialogResult ShowDialog(ref OpenFileDialogParameters parameters);
}

partial class TaskInterfaceExtensions
{
	public static SetupDialogResult ShowDialogWithOkFallback(this ITaskInterface taskInterface, string title, string message, SetupMessageBoxButtons buttons, SetupMessageBoxIcon icon)
	{
		return taskInterface is not IDialogTaskInterface dialogTaskInterface ? SetupDialogResult.Ok : dialogTaskInterface.ShowDialog(title, message, buttons, icon);
	}
	
	public static SetupDialogResult ShowDialogWithOkFallback(this ITaskInterface taskInterface, ref OpenFileDialogParameters parameters)
	{
		return taskInterface is not IDialogTaskInterface dialogTaskInterface ? SetupDialogResult.Ok : dialogTaskInterface.ShowDialog(ref parameters);
	}
}
