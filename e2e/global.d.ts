// Minimal ambient declarations so `process.env` works without @types/node.
declare const process: {
  env: Record<string, string | undefined>;
};
