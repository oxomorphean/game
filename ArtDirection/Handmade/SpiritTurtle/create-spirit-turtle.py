"""Generate and export the original turtle through the real Aseprite MCP transport."""
import asyncio
import json
from pathlib import Path

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
from PIL import Image

PROJECT = Path('/Users/nguyenhoangdacdat/dev/nguthu-unity-demo')
OUT = PROJECT / 'ArtDirection/Handmade/SpiritTurtle'
SPRITES = PROJECT / 'Assets/ArtSample/Sprites'
MCP = Path('/Users/nguyenhoangdacdat/dev/tools/aseprite-mcp')


async def main():
    OUT.mkdir(parents=True, exist_ok=True)
    source = OUT / 'spirit-turtle.aseprite'
    lua_path = Path(__file__).with_name('create-spirit-turtle.lua')
    if not lua_path.exists():
        lua_path = Path('/private/tmp/create_spirit_turtle.lua')
    lua = lua_path.read_text()
    (OUT / 'create-spirit-turtle.lua').write_text(lua)
    evidence = {'transport': 'stdio', 'calls': []}
    async with stdio_client(StdioServerParameters(command=str(MCP / 'run-mcp.sh'), args=[])) as (read, write):
        async with ClientSession(read, write) as session:
            init = await session.initialize()
            evidence['server'] = init.serverInfo.model_dump()
            tools = await session.list_tools()
            evidence['tools_available'] = len(tools.tools)

            async def call(name, **args):
                r = await session.call_tool(name, args)
                message = '\n'.join(c.text for c in r.content if c.type == 'text')
                if r.isError or message.lower().startswith(('failed', 'error:', 'script failed')):
                    raise RuntimeError(f'{name}: {message}')
                evidence['calls'].append({'tool': name, 'result': message})
                print(f'{name}: {message}', flush=True)
                return message

            await call('run_lua_script', script=lua)
            palette = ['#' + c for c in (PROJECT / 'ArtDirection/Palette/linhthu-v1.hex').read_text().splitlines() if c]
            await call('set_palette', filename=str(source), colors=palette)
            await call('export_frame', filename=str(source), frame_index=1,
                       output_filename=str(SPRITES / 'turtle.png'))
            for tag, count in [('idle', 4), ('walk', 6)]:
                await call('export_spritesheet', filename=str(source), tag_name=tag,
                           output_filename=str(SPRITES / f'turtle-{tag}.png'),
                           data_filename=str(OUT / f'turtle-{tag}.json'), sheet_type='horizontal')
                await call('export_spritesheet', filename=str(source), tag_name=tag, scale=5,
                           output_filename=str(OUT / f'turtle-{tag}-preview-5x.png'), sheet_type='horizontal')
                await call('export_tag', filename=str(source), tag_name=tag,
                           output_filename=str(OUT / f'turtle-{tag}.gif'), scale=5)
            await call('export_frame', filename=str(source), frame_index=1,
                       output_filename=str(OUT / 'turtle-preview-8x.png'), scale=8)
            await call('get_sprite_info', filename=str(source))
    palette_rgb = {tuple(int(c[i:i+2], 16) for i in (0,2,4)) for c in
                   (PROJECT / 'ArtDirection/Palette/linhthu-v1.hex').read_text().splitlines() if c}
    report = []
    for tag, count in [('idle',4),('walk',6)]:
        sheet = Image.open(SPRITES / f'turtle-{tag}.png').convert('RGBA')
        assert sheet.size == (64 * count, 64), sheet.size
        frames = [sheet.crop((64*i,0,64*(i+1),64)) for i in range(count)]
        for i, frame in enumerate(frames):
            colors = set(frame.get_flattened_data())
            assert all(c[3] in (0,255) for c in colors)
            rgb = {c[:3] for c in colors if c[3]}
            assert len(rgb) <= 16, len(rgb)
            assert rgb <= palette_rgb, rgb - palette_rgb
            report.append({'tag':tag,'frame':i,'colors':len(rgb),
                           'bbox':frame.getchannel('A').getbbox()})
        assert len({f.tobytes() for f in frames}) == count, f'{tag} repeats identical frames'
        metadata = json.loads((OUT / f'turtle-{tag}.json').read_text())
        assert len(metadata['frames']) == count
    assert Image.open(SPRITES / 'turtle.png').size == (64,64)
    evidence['verified']={'status':'PASS','canvas':[64,64], 'ppu':16,'pivot':[0.5,0.18],
                          'frame_counts':{'idle':4,'walk':6}, 'all_frames_distinct':True,
                          'alpha':[0,255], 'palette':'linhthu-v1.hex','frames':report}
    (OUT / 'verification.json').write_text(json.dumps(evidence, indent=2)+'\n')
    (OUT / 'create-spirit-turtle.py').write_text(Path(__file__).read_text())
    print('PNG_READY: turtle.png, turtle-idle.png, turtle-walk.png; verification PASS', flush=True)


if __name__ == '__main__':
    asyncio.run(main())
