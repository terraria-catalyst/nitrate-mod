import {createPublicizer} from "publicizer";

export const publicizer = createPublicizer("Nitrate");

publicizer.createAssembly("tModLoader").allowVirtuals().publicizeAll();