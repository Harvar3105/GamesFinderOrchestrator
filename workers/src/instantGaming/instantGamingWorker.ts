import { rabbitConn } from "../utils/config.js";

async function startInstantGamingWorker() {
  const channel = await rabbitConn.createChannel();
}