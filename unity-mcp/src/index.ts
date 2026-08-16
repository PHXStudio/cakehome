#!/usr/bin/env node
/**
 * Unity MCP — read-only analysis of a Unity project on disk.
 * Set UNITY_PROJECT_ROOT (or place this package as <project>/unity-mcp).
 */
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

function resolveProjectRoot(): string {
  const env = process.env.UNITY_PROJECT_ROOT?.trim();
  if (env && fs.existsSync(env)) return path.resolve(env);

  // unity-mcp lives at <project>/unity-mcp/{src,dist}
  const candidate = path.resolve(__dirname, "..", "..");
  if (fs.existsSync(path.join(candidate, "Assets")) &&
      fs.existsSync(path.join(candidate, "ProjectSettings"))) {
    return candidate;
  }

  throw new Error(
    "UNITY_PROJECT_ROOT is not set and parent folder is not a Unity project. " +
      "Export UNITY_PROJECT_ROOT=/path/to/UnityProject"
  );
}

const PROJECT_ROOT = resolveProjectRoot();
const ASSETS = path.join(PROJECT_ROOT, "Assets");

function textResult(data: unknown) {
  const text = typeof data === "string" ? data : JSON.stringify(data, null, 2);
  return { content: [{ type: "text" as const, text }] };
}

function errorResult(message: string) {
  return {
    content: [{ type: "text" as const, text: message }],
    isError: true as const,
  };
}

function relFromProject(abs: string): string {
  return path.relative(PROJECT_ROOT, abs).split(path.sep).join("/");
}

function resolveUnderProject(relPath: string): string {
  const cleaned = relPath.replace(/^[/\\]+/, "");
  const abs = path.resolve(PROJECT_ROOT, cleaned);
  if (!abs.startsWith(PROJECT_ROOT + path.sep) && abs !== PROJECT_ROOT) {
    throw new Error(`Path escapes project root: ${relPath}`);
  }
  return abs;
}

function walkFiles(
  dir: string,
  filter: (name: string, abs: string) => boolean,
  out: string[] = [],
  max = 50_000
): string[] {
  if (!fs.existsSync(dir) || out.length >= max) return out;
  let entries: fs.Dirent[];
  try {
    entries = fs.readdirSync(dir, { withFileTypes: true });
  } catch {
    return out;
  }
  for (const ent of entries) {
    if (out.length >= max) break;
    if (ent.name.startsWith(".")) continue;
    const abs = path.join(dir, ent.name);
    if (ent.isDirectory()) {
      // Skip heavy / non-asset trees
      if (["Library", "Temp", "Obj", "Logs", "node_modules", ".git", "unity-mcp"].includes(ent.name)) {
        continue;
      }
      walkFiles(abs, filter, out, max);
    } else if (ent.isFile() && filter(ent.name, abs)) {
      out.push(abs);
    }
  }
  return out;
}

const EXT_MAP: Record<string, string[]> = {
  scene: [".unity"],
  prefab: [".prefab"],
  script: [".cs"],
  asset: [".asset"],
  material: [".mat"],
  texture: [".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr", ".hdr", ".webp"],
  model: [".fbx", ".obj", ".blend", ".dae", ".glb", ".gltf"],
  animation: [".anim", ".controller", ".overrideController"],
  audio: [".wav", ".mp3", ".ogg", ".aiff"],
  shader: [".shader", ".shadergraph", ".compute", ".hlsl", ".cginc"],
};

function matchType(fileName: string, type: string): boolean {
  if (type === "all") return !fileName.endsWith(".meta");
  const exts = EXT_MAP[type];
  if (!exts) return false;
  const lower = fileName.toLowerCase();
  return exts.some((e) => lower.endsWith(e));
}

function readTextLimited(abs: string, maxBytes = 2_000_000): string {
  const st = fs.statSync(abs);
  if (st.size > maxBytes) {
    return fs.readFileSync(abs, "utf8").slice(0, maxBytes) +
      `\n\n… truncated (${st.size} bytes total)`;
  }
  return fs.readFileSync(abs, "utf8");
}

function parseYamlLikeObjects(text: string): Array<Record<string, string>> {
  // Unity YAML is not strict multi-doc for components; extract GO names + component types.
  const objects: Array<Record<string, string>> = [];
  const blocks = text.split(/^--- !u!/m).slice(1);
  for (const block of blocks) {
    const typeMatch = block.match(/^(\d+)\s*&(\d+)/);
    const fileId = typeMatch?.[1] ?? "";
    const id = typeMatch?.[2] ?? "";
    const nameMatch = block.match(/^\s*m_Name:\s*(.+)$/m);
    const classMatch = block.match(/^\s*m_Script:.*guid:\s*([a-f0-9]+)/m);
    const goMatch = block.match(/^GameObject:/m);
    const transformMatch = block.match(/^Transform:/m);
    const prefabMatch = block.match(/^PrefabInstance:/m);
    const monoMatch = block.match(/^MonoBehaviour:/m);
    const record: Record<string, string> = { fileId, id };
    if (nameMatch) record.name = nameMatch[1].trim();
    if (goMatch) record.kind = "GameObject";
    else if (transformMatch) record.kind = "Transform";
    else if (prefabMatch) record.kind = "PrefabInstance";
    else if (monoMatch) record.kind = "MonoBehaviour";
    else {
      const firstLine = block.split("\n").find((l) => l.trim() && !l.startsWith("&") && !/^\d/.test(l.trim()));
      if (firstLine) record.kind = firstLine.replace(":", "").trim();
    }
    if (classMatch) record.scriptGuid = classMatch[1];
    objects.push(record);
  }
  return objects;
}

function summarizeUnityYaml(abs: string, label: string) {
  const text = readTextLimited(abs);
  const objects = parseYamlLikeObjects(text);
  const gameObjects = objects.filter((o) => o.kind === "GameObject" && o.name);
  const mono = objects.filter((o) => o.kind === "MonoBehaviour");
  const prefabInstances = objects.filter((o) => o.kind === "PrefabInstance");
  return {
    path: relFromProject(abs),
    label,
    sizeBytes: fs.statSync(abs).size,
    counts: {
      totalBlocks: objects.length,
      gameObjects: gameObjects.length,
      monoBehaviours: mono.length,
      prefabInstances: prefabInstances.length,
    },
    gameObjects: gameObjects.slice(0, 200).map((g) => g.name),
    monoScriptGuids: [...new Set(mono.map((m) => m.scriptGuid).filter(Boolean))].slice(0, 100),
  };
}

function projectInfo() {
  const versionPath = path.join(PROJECT_ROOT, "ProjectSettings", "ProjectVersion.txt");
  let editorVersion = "unknown";
  if (fs.existsSync(versionPath)) {
    const m = fs.readFileSync(versionPath, "utf8").match(/m_EditorVersion:\s*(\S+)/);
    if (m) editorVersion = m[1];
  }

  let packages: string[] = [];
  const manifestPath = path.join(PROJECT_ROOT, "Packages", "manifest.json");
  if (fs.existsSync(manifestPath)) {
    try {
      const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8")) as {
        dependencies?: Record<string, string>;
      };
      packages = Object.entries(manifest.dependencies ?? {}).map(
        ([name, ver]) => `${name}@${ver}`
      );
    } catch {
      packages = [];
    }
  }

  const counts: Record<string, number> = {};
  for (const t of Object.keys(EXT_MAP)) {
    counts[t] = walkFiles(ASSETS, (n) => matchType(n, t)).length;
  }
  counts.all = walkFiles(ASSETS, (n) => matchType(n, "all")).length;

  return {
    projectRoot: PROJECT_ROOT,
    editorVersion,
    packages,
    assetCounts: counts,
  };
}

function listAssets(type: string, subDir?: string) {
  const root = subDir
    ? resolveUnderProject(path.join("Assets", subDir.replace(/^Assets[/\\]?/, "")))
    : ASSETS;
  if (!fs.existsSync(root)) {
    throw new Error(`Directory not found: ${relFromProject(root)}`);
  }
  const files = walkFiles(root, (n) => matchType(n, type));
  const rels = files.map(relFromProject).sort();
  return {
    type,
    subDir: subDir ?? "",
    count: rels.length,
    paths: rels.slice(0, 2000),
    truncated: rels.length > 2000,
  };
}

function searchScripts(pattern: string, filePattern?: string) {
  let regex: RegExp;
  try {
    regex = new RegExp(pattern, "gi");
  } catch (e) {
    throw new Error(`Invalid regex: ${(e as Error).message}`);
  }
  const ext = (filePattern || ".cs").toLowerCase();
  const files = walkFiles(ASSETS, (n) => n.toLowerCase().endsWith(ext) && !n.endsWith(".meta"));
  const hits: Array<{ path: string; line: number; text: string }> = [];
  const maxHits = 200;
  for (const abs of files) {
    if (hits.length >= maxHits) break;
    let content: string;
    try {
      content = fs.readFileSync(abs, "utf8");
    } catch {
      continue;
    }
    const lines = content.split(/\r?\n/);
    for (let i = 0; i < lines.length; i++) {
      regex.lastIndex = 0;
      if (regex.test(lines[i])) {
        hits.push({
          path: relFromProject(abs),
          line: i + 1,
          text: lines[i].trim().slice(0, 300),
        });
        if (hits.length >= maxHits) break;
      }
    }
  }
  return {
    pattern,
    filePattern: ext,
    matchCount: hits.length,
    truncated: hits.length >= maxHits,
    matches: hits,
  };
}

function findReferences(target: string) {
  let guid = target.trim();
  let fromPath: string | undefined;
  if (!/^[a-f0-9]{32}$/i.test(guid)) {
    const abs = resolveUnderProject(target);
    const metaPath = abs.endsWith(".meta") ? abs : abs + ".meta";
    if (!fs.existsSync(metaPath)) {
      throw new Error(`No .meta for target: ${target}`);
    }
    const meta = fs.readFileSync(metaPath, "utf8");
    const m = meta.match(/guid:\s*([a-f0-9]{32})/i);
    if (!m) throw new Error(`GUID not found in ${relFromProject(metaPath)}`);
    guid = m[1];
    fromPath = relFromProject(abs.endsWith(".meta") ? abs.slice(0, -5) : abs);
  }

  const searchRoots = [
    ASSETS,
    path.join(PROJECT_ROOT, "ProjectSettings"),
    path.join(PROJECT_ROOT, "Packages"),
  ].filter(fs.existsSync);

  const refs: string[] = [];
  const maxRefs = 500;
  const textExts = new Set([
    ".prefab", ".unity", ".asset", ".mat", ".controller", ".anim",
    ".overrideController", ".physicMaterial", ".physicsMaterial2D",
    ".mask", ".playable", ".signal", ".spriteatlas", ".shadergraph",
    ".shadersubgraph", ".cs", ".asmdef", ".inputactions", ".json",
    ".txt", ".xml", ".yml", ".yaml", ".compute", ".shader", ".hlsl",
  ]);

  for (const root of searchRoots) {
    const files = walkFiles(root, (n, abs) => {
      if (n.endsWith(".meta")) return false;
      const e = path.extname(n).toLowerCase();
      return textExts.has(e) || e === "";
    });
    for (const abs of files) {
      if (refs.length >= maxRefs) break;
      let content: string;
      try {
        const st = fs.statSync(abs);
        if (st.size > 8_000_000) continue;
        content = fs.readFileSync(abs, "utf8");
      } catch {
        continue;
      }
      if (content.includes(guid)) {
        refs.push(relFromProject(abs));
      }
    }
    if (refs.length >= maxRefs) break;
  }

  return {
    guid,
    targetPath: fromPath,
    referenceCount: refs.length,
    truncated: refs.length >= maxRefs,
    references: refs,
  };
}

function readMeta(assetPath: string) {
  const abs = resolveUnderProject(assetPath);
  const metaPath = abs.endsWith(".meta") ? abs : abs + ".meta";
  if (!fs.existsSync(metaPath)) {
    throw new Error(`Meta not found: ${relFromProject(metaPath)}`);
  }
  return {
    path: relFromProject(metaPath),
    content: readTextLimited(metaPath, 500_000),
  };
}

function readAssetFile(rel: string) {
  const abs = resolveUnderProject(rel);
  if (!fs.existsSync(abs)) throw new Error(`Not found: ${rel}`);
  const lower = abs.toLowerCase();
  if (lower.endsWith(".prefab") || lower.endsWith(".unity")) {
    return summarizeUnityYaml(abs, lower.endsWith(".prefab") ? "prefab" : "scene");
  }
  return {
    path: relFromProject(abs),
    sizeBytes: fs.statSync(abs).size,
    content: readTextLimited(abs, 500_000),
  };
}

const server = new McpServer({
  name: "unity-mcp",
  version: "1.0.0",
});

server.tool(
  "unity_project_info",
  "Get Unity project overview: version, packages, asset counts",
  {},
  async () => {
    try {
      return textResult(projectInfo());
    } catch (e) {
      return errorResult((e as Error).message);
    }
  }
);

server.tool(
  "unity_list_assets",
  "List Unity assets by type (scene, prefab, script, asset, material, texture, model, animation, audio, shader, all)",
  {
    type: z
      .string()
      .describe(
        "Asset type: scene, prefab, script, asset, material, texture, model, animation, audio, shader, all"
      ),
    subDir: z
      .string()
      .optional()
      .describe("Optional subdirectory within Assets to search"),
  },
  async ({ type, subDir }) => {
    try {
      return textResult(listAssets(type, subDir));
    } catch (e) {
      return errorResult((e as Error).message);
    }
  }
);

server.tool(
  "unity_read_scene",
  "Parse and read a Unity scene file - shows GameObjects, components, and MonoBehaviours",
  {
    path: z.string().describe("Relative path to the .unity scene file"),
  },
  async ({ path: p }) => {
    try {
      return textResult(summarizeUnityYaml(resolveUnderProject(p), "scene"));
    } catch (e) {
      return errorResult((e as Error).message);
    }
  }
);

server.tool(
  "unity_read_prefab",
  "Parse and read a Unity prefab file - shows GameObjects and components",
  {
    path: z.string().describe("Relative path to the .prefab file"),
  },
  async ({ path: p }) => {
    try {
      return textResult(summarizeUnityYaml(resolveUnderProject(p), "prefab"));
    } catch (e) {
      return errorResult((e as Error).message);
    }
  }
);

server.tool(
  "unity_read_asset",
  "Read a Unity asset file (.asset, .mat, etc.) and show its structure",
  {
    path: z.string().describe("Relative path to the asset file"),
  },
  async ({ path: p }) => {
    try {
      return textResult(readAssetFile(p));
    } catch (e) {
      return errorResult((e as Error).message);
    }
  }
);

server.tool(
  "unity_find_references",
  "Find all references to an asset by GUID or file path across the project",
  {
    target: z
      .string()
      .describe("GUID (32 hex chars) or relative file path to search for"),
  },
  async ({ target }) => {
    try {
      return textResult(findReferences(target));
    } catch (e) {
      return errorResult((e as Error).message);
    }
  }
);

server.tool(
  "unity_search_scripts",
  "Search for a text/regex pattern in C# scripts",
  {
    pattern: z.string().describe("Search pattern (regex supported)"),
    filePattern: z
      .string()
      .optional()
      .describe("Optional file extension filter (e.g. .cs, .shader)"),
  },
  async ({ pattern, filePattern }) => {
    try {
      return textResult(searchScripts(pattern, filePattern));
    } catch (e) {
      return errorResult((e as Error).message);
    }
  }
);

server.tool(
  "unity_read_meta",
  "Read a Unity .meta file to get GUID and import settings",
  {
    path: z
      .string()
      .describe("Relative path to the asset (not the .meta file)"),
  },
  async ({ path: p }) => {
    try {
      return textResult(readMeta(p));
    } catch (e) {
      return errorResult((e as Error).message);
    }
  }
);

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error(`Unity MCP Server running for project: ${PROJECT_ROOT}`);
}

main().catch((err) => {
  console.error("unity-mcp failed:", err);
  process.exit(1);
});
