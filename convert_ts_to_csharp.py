import os, re, sys

DIR_MAP = {
    'constants': 'ClaudeCode.Constants',
    'types': 'ClaudeCode.Core/Types',
    'tools': 'ClaudeCode.Tools',
    'services': 'ClaudeCode.Services',
    'commands': 'ClaudeCode.Commands',
    'cli': 'ClaudeCode.Cli/Cli',
    'utils': 'ClaudeCode.Core/Utils',
    'components': 'ClaudeCode.Cli/Components',
    'keybindings': 'ClaudeCode.Core/Keybindings',
    'vim': 'ClaudeCode.Core/Vim',
    'bridge': 'ClaudeCode.Services/Bridge',
    'buddy': 'ClaudeCode.Services/Buddy',
    'coordinator': 'ClaudeCode.Services/Coordinator',
    'context': 'ClaudeCode.Core/Context',
    'state': 'ClaudeCode.Core/State',
    'schemas': 'ClaudeCode.Core/Schemas',
    'hooks': 'ClaudeCode.Core/Hooks',
    'memdir': 'ClaudeCode.Core/Memdir',
    'migrations': 'ClaudeCode.Core/Migrations',
    'entrypoints': 'ClaudeCode.Cli/Entrypoints',
    'outputStyles': 'ClaudeCode.Core/OutputStyles',
    'query': 'ClaudeCode.Services/Query',
    'remote': 'ClaudeCode.Services/Remote',
    'server': 'ClaudeCode.Services/Server',
    'skills': 'ClaudeCode.Services/Skills',
    'tasks': 'ClaudeCode.Services/Tasks',
    'voice': 'ClaudeCode.Services/Voice',
    'upstreamproxy': 'ClaudeCode.Services/UpstreamProxy',
    'ink': 'ClaudeCode.Cli/Ink',
    'moreright': 'ClaudeCode.Cli/Moreright',
    'native-ts': 'ClaudeCode.Core/NativeTs',
    'plugins': 'ClaudeCode.Services/Plugins',
}

SRC = '/home/runner/work/claude-code/claude-code/src'
CS_BASE = '/home/runner/work/claude-code/claude-code/csharp/src'

def get_namespace(rel_path):
    parts = rel_path.replace('\\','/').split('/')
    top = parts[0] if parts else ''
    proj = DIR_MAP.get(top, 'ClaudeCode.Core').replace('/', '.')
    sub = '.'.join(p for p in parts[1:-1] if p)
    return (proj + '.' + sub).rstrip('.') if sub else proj

def get_cs_path(rel_path):
    parts = rel_path.replace('\\','/').split('/')
    top = parts[0]
    proj_sub = DIR_MAP.get(top, '')
    if proj_sub:
        proj = proj_sub.split('/')[0]
        sub = proj_sub[len(proj)+1:] if '/' in proj_sub else ''
        rest = parts[1:]
    else:
        proj = 'ClaudeCode.Core'
        sub = ''
        rest = parts
    fname = os.path.splitext(rest[-1])[0] + '.cs' if rest else 'Unknown.cs'
    dirs = ([sub] if sub else []) + [p for p in rest[:-1] if p]
    return os.path.join(CS_BASE, proj, *dirs, fname)

def convert(content, filename, namespace):
    lines = content.split('\n')
    out = []
    
    # Collect using/import lines
    usings = set(['System', 'System.Collections.Generic', 'System.Threading.Tasks'])
    
    for line in lines:
        # Skip import lines
        if re.match(r'\s*import\s+', line):
            if 'fs' in line or 'path' in line: usings.add('System.IO')
            if 'http' in line.lower(): usings.add('System.Net.Http')
            continue
        if re.match(r'\s*export\s*\{', line): continue
        if re.match(r'\s*"use strict"', line): continue
        if re.match(r'\s*require\(', line): continue
        
        # export const
        m = re.match(r'\s*export\s+const\s+(\w+)\s*=\s*(.+?)(\s*;?\s*)$', line)
        if m:
            name, val, _ = m.groups()
            val = val.strip().rstrip(';')
            if val.startswith('"') or val.startswith("'"):
                line = f'    public const string {name} = {val.replace(chr(39), chr(34))};'
            elif re.match(r'^-?\d+$', val):
                line = f'    public const int {name} = {val};'
            elif re.match(r'^-?\d+\.\d+$', val):
                line = f'    public const double {name} = {val};'
            elif val in ('true','false'):
                line = f'    public const bool {name} = {val};'
            else:
                val = convert_type_expr(val)
                line = f'    public static readonly object {name} = {val};'
        
        # export interface
        line = re.sub(r'\bexport\s+interface\s+', 'public interface ', line)
        # export type X = 
        line = re.sub(r'\bexport\s+type\s+(\w+)\s*=', r'// type \1 =', line)
        # export enum
        line = re.sub(r'\bexport\s+enum\s+', 'public enum ', line)
        # export abstract class
        line = re.sub(r'\bexport\s+abstract\s+class\s+', 'public abstract class ', line)
        # export class
        line = re.sub(r'\bexport\s+class\s+', 'public class ', line)
        # export default class
        line = re.sub(r'\bexport\s+default\s+class\s+', 'public class ', line)
        # export default function
        line = re.sub(r'\bexport\s+default\s+function\s*\*?\s*', 'public static ', line)
        # export function
        line = re.sub(r'\bexport\s+function\s*\*?\s*', 'public static ', line)
        # export async function
        line = re.sub(r'\bexport\s+async\s+function\s*\*?\s*', 'public static async ', line)
        # export default
        line = re.sub(r'\bexport\s+default\s+', '// export default ', line)
        # export {
        line = re.sub(r'\bexport\s+\{[^}]*\}', '// exports', line)
        
        # Type conversions
        line = re.sub(r'\bstring\s*\|\s*null\b', 'string?', line)
        line = re.sub(r'\bnumber\s*\|\s*null\b', 'double?', line)
        line = re.sub(r'\bboolean\s*\|\s*null\b', 'bool?', line)
        line = re.sub(r'\b(?<!\w)number\b(?!\w)', 'double', line)
        line = re.sub(r'\bboolean\b', 'bool', line)
        line = re.sub(r'\bany\b', 'object', line)
        line = re.sub(r'\bunknown\b', 'object?', line)
        line = re.sub(r'\bvoid\b', 'void', line)
        line = re.sub(r'\bPromise<([^>]+)>', r'Task<\1>', line)
        line = re.sub(r'\bArray<([^>]+)>', r'List<\1>', line)
        line = re.sub(r'\bRecord<([^,>]+),\s*([^>]+)>', r'Dictionary<\1,\2>', line)
        line = re.sub(r'\bMap<([^,>]+),\s*([^>]+)>', r'Dictionary<\1,\2>', line)
        line = re.sub(r'\bSet<([^>]+)>', r'HashSet<\1>', line)
        line = re.sub(r'(\w+)\[\]', r'List<\1>', line)
        line = re.sub(r'\breadonly\s+List<', 'IReadOnlyList<', line)
        
        # Template literals
        line = re.sub(r'`([^`]*)`', lambda m: '$"' + m.group(1).replace('${', '{') + '"', line)
        
        # console
        line = re.sub(r'\bconsole\.log\b', 'Console.WriteLine', line)
        line = re.sub(r'\bconsole\.error\b', 'Console.Error.WriteLine', line)
        line = re.sub(r'\bconsole\.warn\b', 'Console.Error.WriteLine', line)
        
        # process
        line = re.sub(r'\bprocess\.exit\(', 'Environment.Exit(', line)
        line = re.sub(r'\bprocess\.env\.(\w+)', r'Environment.GetEnvironmentVariable("\1")', line)
        
        # JSON
        line = re.sub(r'\bJSON\.stringify\b', 'JsonSerializer.Serialize', line)
        line = re.sub(r'\bJSON\.parse\b', 'JsonSerializer.Deserialize<object>', line)
        
        # as const, satisfies
        line = re.sub(r'\s+as\s+const\b', '', line)
        line = re.sub(r'\s+satisfies\s+\w+', '', line)
        
        # Type assertions
        line = re.sub(r'\s+as\s+(\w+)', r' /* as \1 */', line)
        
        # private/public/protected modifiers
        line = re.sub(r'\bprivate\s+readonly\b', 'private readonly', line)
        
        # readonly field
        line = re.sub(r'\breadonly\s+(\w)', r'readonly \1', line)
        
        # Optional chaining - basic
        # Remove TypeScript-only syntax
        line = re.sub(r':\s*never\b', ': object /* never */', line)
        
        out.append(line)
    
    body = '\n'.join(out)
    
    class_name = os.path.splitext(os.path.basename(filename))[0]
    class_name = re.sub(r'[^a-zA-Z0-9_]', '_', class_name)
    if class_name[0].isdigit(): class_name = '_' + class_name
    
    using_str = '\n'.join(f'using {u};' for u in sorted(usings))
    
    result = f"""{using_str}

namespace {namespace};

// Converted from {filename}
{body}
"""
    return result

def convert_type_expr(val):
    val = re.sub(r'\bArray<([^>]+)>', r'List<\1>', val)
    val = re.sub(r'\bRecord<([^,>]+),\s*([^>]+)>', r'Dictionary<\1,\2>', val)
    return val

created = 0
skipped = 0

for root, dirs, files in os.walk(SRC):
    dirs[:] = [d for d in dirs if d != 'node_modules' and not d.startswith('.')]
    for fname in files:
        if not (fname.endswith('.ts') or fname.endswith('.tsx')):
            continue
        if fname.endswith('.d.ts'):
            continue
        ts_path = os.path.join(root, fname)
        rel = os.path.relpath(ts_path, SRC)
        cs_path = get_cs_path(rel)
        namespace = get_namespace(rel)
        
        if os.path.exists(cs_path):
            skipped += 1
            continue
        
        os.makedirs(os.path.dirname(cs_path), exist_ok=True)
        try:
            with open(ts_path, 'r', encoding='utf-8', errors='replace') as f:
                content = f.read()
            cs_content = convert(content, fname, namespace)
            with open(cs_path, 'w', encoding='utf-8') as f:
                f.write(cs_content)
            created += 1
        except Exception as e:
            print(f"ERROR {ts_path}: {e}")

print(f"Created: {created}, Skipped: {skipped}")
