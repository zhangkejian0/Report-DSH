"""One-off MCP stdio smoke against the real SDK executables."""
import json
import os
import subprocess

proc = subprocess.Popen(
    ['node', 'fotric_sdk/mcp/thermal-mcp.mjs'],
    stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')


def send(msg):
    proc.stdin.write(json.dumps(msg) + '\n')
    proc.stdin.flush()
    line = proc.stdout.readline()
    return json.loads(line) if line.strip() else None


info = send({'jsonrpc': '2.0', 'id': 1, 'method': 'initialize', 'params': {'protocolVersion': '2025-06-18'}})
print('initialize:', info['result']['serverInfo'])
tools = send({'jsonrpc': '2.0', 'id': 2, 'method': 'tools/list'})['result']['tools']
print('tools:', [t['name'] for t in tools])

img = r'C:\Users\Zhangkejian\Desktop\IR\1772517841738-1772517690911-IR_20210306_0007.jpg'

r = send({'jsonrpc': '2.0', 'id': 3, 'method': 'tools/call', 'params': {'name': 'thermal_analyze', 'arguments': {'image_path': img}}})
v = json.loads(r['result']['content'][0]['text'])
print('analyze: size=%sx%s max=%s markers=%d' % (v['file']['width'], v['file']['height'], v['stats']['max'], len(v['markers'])))

r = send({'jsonrpc': '2.0', 'id': 4, 'method': 'tools/call', 'params': {'name': 'thermal_render', 'arguments': {'image_path': img}}})
image_blocks = [c for c in r['result']['content'] if c['type'] == 'image']
print('render: image block', image_blocks[0]['mimeType'], 'b64len=', len(image_blocks[0]['data']))

mark_args = {'image_path': img, 'markers': [{'type': 'rectangle', 'name': '绝缘子1', 'rect': [37, 103, 113, 81]}]}
r = send({'jsonrpc': '2.0', 'id': 5, 'method': 'tools/call', 'params': {'name': 'thermal_mark', 'arguments': mark_args}})
m = json.loads(r['result']['content'][0]['text'])
print('mark:', m['ok'], m['markers'][0]['name'], m['markers'][0]['max'])

edit_args = {'image_path': img, 'edits': {'params': {'emissivity': 0.9}, 'note': 'MCP测试'}}
r = send({'jsonrpc': '2.0', 'id': 6, 'method': 'tools/call', 'params': {'name': 'thermal_edit', 'arguments': edit_args}})
e = json.loads(r['result']['content'][0]['text'])
print('edit:', e['ok'], e['params']['emissivity'], e['note'], '->', os.path.basename(e['dst']))

r = send({'jsonrpc': '2.0', 'id': 7, 'method': 'tools/call', 'params': {'name': 'thermal_analyze', 'arguments': {'image_path': 'no-such.jpg'}}})
print('missing file isError:', r['result'].get('isError'), r['result']['content'][0]['text'][:50])

proc.stdin.close()
proc.wait(timeout=10)
print('MCP SMOKE OK')
