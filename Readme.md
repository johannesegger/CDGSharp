# CDGSharp

[![Nuget](https://img.shields.io/nuget/v/CDGSharp)](https://www.nuget.org/packages/CDGSharp)

CDGSharp is a [CD+G](https://jbum.com/cdg_revealed.html) parser, serializer and karaoke generator.
It can also be used to better understand a .cdg file by formatting, explaining, or visualizing each CD+G packet.

CDGSharp basically consists of three parts:
    * a [NuGet package](https://www.nuget.org/packages/CDGSharp) to use and/or extend CDGSharp's capabilities.
    * a [CLI](#cli) for inspecting a .cdg file and generating .cdg files from .lrc files (lyrics + timestamps + metadata)
    * a [LyricsGenerator](#lyrics-generator) console application for generating a basic .lrc file by adding timestamps to lyrics

## CLI

The CLI is probably best explored by running the help command (thanks to [Argu](https://github.com/fsprojects/Argu)).

```
> dotnet run --project .\CDGSharp.CLI\ -- --help
USAGE: CDGSharp.CLI.exe [--help] [<subcommand> [<options>]]

SUBCOMMANDS:

    format <options>      Format packets of a .cdg file.
    explain <options>     Explain packets of a .cdg file.
    render-images <options>
                          Render images of a .cdg file.
    convert-lrc <options> Convert an .lrc file into a .cdg file.

    Use 'CDGSharp.CLI.exe <subcommand> --help' for additional information.

OPTIONS:

    --help                display this list of options.
```

## API via NuGet package

The API basically has the same capabilities as the CLI plus:

* `Renderer` (`open CDG.Renderer`): Enables continuous application of CD+G packets to update a visualization state (i.e. color table, color indices per pixel, border color index). Use `Renderer.render` to apply multiple packets or `Renderer.applyPacket`/`Renderer.applyCDGPacket` to apply a single packet.
* `Serializer` (`open CDG.Serializer`): Serialize CD+G packets. The result can be written into a .cdg file.
* `LrcFile` (`open CDG.LrcParser`) and `LrcToKaraoke` (`open CDG.LrcToKaraoke`): Parse .lrc files and convert them for easier transformation to .cdg karaoke files. Use `LrcFile.parseFile` and `LrcToKaraoke.getKaraokeCommands` to get started.

## Lyrics generator

Also probably best explored by running the help command (thanks again to [Argu](https://github.com/fsprojects/Argu)).

```
> dotnet run --project .\CDGSharp.LyricsGenerator\ -- --help
USAGE: CDGSharp.LyricsGenerator.exe [--help] [--lyrics-path <string>] [--audio-path <string>] [--target-path <string>] [--speed-factor <double>]

OPTIONS:

    --lyrics-path <string>
                          Path to a text file that contains the raw lyrics.
    --audio-path <string> Path to the audio file that is played during generation of the .lrc file.
    --target-path <string>
                          Path to the generated .lrc file.
    --speed-factor <double>
                          Controls the speed of the audio file, defaults to '0.5'.
    --help                display this list of options.
```

Note that when generating .cdg karaoke files you'll have to manually add the artist (`[ar:The Beatles]`) and the title (`[ti:Yesterday]`) followed by a blank line to the .lrc file.

# CD+G projects that I found useful

* https://github.com/gyunaev/karlyriceditor - Similar to this project with some additional features like inserting images. However the generated .cdg karaoke file didn't render properly on the device I used.
* https://sourceforge.net/projects/cdgeditor/ - Inspecting a .cdg file packet by packet.
