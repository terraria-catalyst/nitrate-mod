import {createPublicizer} from "publicizer";

export const publicizer = createPublicizer("Nitrate");

publicizer.createAssembly("tModLoader").publicizeAll();
publicizer.createAssembly("FNA").publicizeAll();