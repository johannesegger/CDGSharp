module CDG.Test.LrcToKaraoke

open CDG
open CDG.ImageProcessing
open CDG.KaraokeGenerator
open CDG.LrcToKaraoke
open CDG.LrcParser
open Expecto
open System

let defaultSettings = {
    BackgroundColor = { Red = ColorChannel 0uy; Green = ColorChannel 0uy; Blue = ColorChannel 0uy }
    DefaultTextColor = { Red = ColorChannel 15uy; Green = ColorChannel 15uy; Blue = ColorChannel 15uy }
    SungTextColor = { Red = ColorChannel 6uy; Green = ColorChannel 6uy; Blue = ColorChannel 6uy }
    DefaultFont = { Type = SystemFont "Arial"; Size = 15 }
}

let tests = testList "LrcToKaraoke" [
    testCase "Single line part" <| fun () ->
        let actualCommands =
            Lyrics [
                [
                    [
                        { StartTime = None; EndTime = Some (TimeSpan.FromSeconds 1.); Text = "Hello" }
                    ]
                ]
            ]
            |> LrcToKaraoke.getShowLyricsPageCommands defaultSettings
        
        let expectedCommands = [
            {
                StartTime = TimeSpan.Zero
                BackgroundColor = defaultSettings.BackgroundColor
                CommandType = ShowLyricsPage {
                    NotSungYetColor = defaultSettings.DefaultTextColor
                    SungColor = defaultSettings.SungTextColor
                    Font = defaultSettings.DefaultFont
                    Lines = [
                        [
                            { Text = "Hello"; Duration = TimeSpan.FromSeconds(1.) }
                        ]
                    ]
                }
            }
        ]
        Expect.equal actualCommands expectedCommands "Expected single page with single line part"

    testCase "Two line parts" <| fun () ->
        let actualCommands =
            Lyrics [
                [
                    [
                        { StartTime = Some (TimeSpan.FromSeconds 0.5); EndTime = Some (TimeSpan.FromSeconds 1.); Text = "Hello" }
                        { StartTime = None; EndTime = Some (TimeSpan.FromSeconds 1.5); Text = "World" }
                    ]
                ]
            ]
            |> LrcToKaraoke.getShowLyricsPageCommands defaultSettings
        
        let expectedCommands = [
            {
                StartTime = TimeSpan.FromSeconds(0.5)
                BackgroundColor = defaultSettings.BackgroundColor
                CommandType = ShowLyricsPage {
                    NotSungYetColor = defaultSettings.DefaultTextColor
                    SungColor = defaultSettings.SungTextColor
                    Font = defaultSettings.DefaultFont
                    Lines = [
                        [
                            { Text = "Hello"; Duration = TimeSpan.FromSeconds(0.5) }
                            { Text = " "; Duration = TimeSpan.Zero }
                            { Text = "World"; Duration = TimeSpan.FromSeconds(0.5) }
                        ]
                    ]
                }
            }
        ]
        Expect.equal actualCommands expectedCommands "Expected single page with two line parts"

    testCase "Two lines" <| fun () ->
        let actualCommands =
            Lyrics [
                [
                    [
                        { StartTime = None; EndTime = Some (TimeSpan.FromSeconds 1.); Text = "Hello" }
                        { StartTime = Some (TimeSpan.FromSeconds(1.2)); EndTime = Some (TimeSpan.FromSeconds 1.5); Text = "World" }
                    ]
                    [
                        { StartTime = Some (TimeSpan.FromSeconds(1.9)); EndTime = Some (TimeSpan.FromSeconds 2.); Text = "How" }
                        { StartTime = Some (TimeSpan.FromSeconds(3.2)); EndTime = Some (TimeSpan.FromSeconds 3.5); Text = "are" }
                        { StartTime = None; EndTime = Some (TimeSpan.FromSeconds 4.); Text = "you?" }
                    ]
                ]
            ]
            |> LrcToKaraoke.getShowLyricsPageCommands defaultSettings
        
        let expectedCommands = [
            {
                StartTime = TimeSpan.Zero
                BackgroundColor = defaultSettings.BackgroundColor
                CommandType = ShowLyricsPage {
                    NotSungYetColor = defaultSettings.DefaultTextColor
                    SungColor = defaultSettings.SungTextColor
                    Font = defaultSettings.DefaultFont
                    Lines = [
                        [
                            { Text = "Hello"; Duration = TimeSpan.FromSeconds(1.) }
                            { Text = " "; Duration = TimeSpan.FromSeconds(0.2) }
                            { Text = "World"; Duration = TimeSpan.FromSeconds(0.3) }
                            { Text = ""; Duration = TimeSpan.FromSeconds(0.4) }
                        ]
                        [
                            { Text = "How"; Duration = TimeSpan.FromSeconds(0.1) }
                            { Text = " "; Duration = TimeSpan.FromSeconds(1.2) }
                            { Text = "are"; Duration = TimeSpan.FromSeconds(0.3) }
                            { Text = " "; Duration = TimeSpan.Zero }
                            { Text = "you?"; Duration = TimeSpan.FromSeconds(0.5) }
                        ]
                    ]
                }
            }
        ]
        Expect.equal actualCommands expectedCommands "Expected single page with two lines"

    testCase "Two pages" <| fun () ->
        let actualCommands =
            Lyrics [
                [
                    [
                        { StartTime = None; EndTime = Some (TimeSpan.FromSeconds 1.); Text = "Hello" }
                        { StartTime = None; EndTime = Some (TimeSpan.FromSeconds 1.5); Text = "World" }
                    ]
                    [
                        { StartTime = None; EndTime = Some (TimeSpan.FromSeconds 2.); Text = "How" }
                        { StartTime = None; EndTime = Some (TimeSpan.FromSeconds 3.5); Text = "are" }
                        { StartTime = None; EndTime = Some (TimeSpan.FromSeconds 4.); Text = "you?" }
                    ]
                ]
                [
                    [
                        { StartTime = Some (TimeSpan.FromSeconds 5.); EndTime = None; Text = "This" }
                        { StartTime = Some (TimeSpan.FromSeconds 6.); EndTime = None; Text = "is" }
                        { StartTime = Some (TimeSpan.FromSeconds 7.); EndTime = None; Text = "page" }
                    ]
                    [
                        { StartTime = Some (TimeSpan.FromSeconds 10.); EndTime = Some (TimeSpan.FromSeconds 10.2); Text = "#2" }
                    ]
                ]
            ]
            |> LrcToKaraoke.getShowLyricsPageCommands defaultSettings
        
        let expectedCommands = [
            {
                StartTime = TimeSpan.Zero
                BackgroundColor = defaultSettings.BackgroundColor
                CommandType = ShowLyricsPage {
                    NotSungYetColor = defaultSettings.DefaultTextColor
                    SungColor = defaultSettings.SungTextColor
                    Font = defaultSettings.DefaultFont
                    Lines = [
                        [
                            { Text = "Hello"; Duration = TimeSpan.FromSeconds(1.) }
                            { Text = " "; Duration = TimeSpan.Zero }
                            { Text = "World"; Duration = TimeSpan.FromSeconds(0.5) }
                        ]
                        [
                            { Text = "How"; Duration = TimeSpan.FromSeconds(0.5) }
                            { Text = " "; Duration = TimeSpan.Zero }
                            { Text = "are"; Duration = TimeSpan.FromSeconds(1.5) }
                            { Text = " "; Duration = TimeSpan.Zero }
                            { Text = "you?"; Duration = TimeSpan.FromSeconds(0.5) }
                        ]
                    ]
                }
            }
            {
                StartTime = TimeSpan.FromSeconds(5.)
                BackgroundColor = defaultSettings.BackgroundColor
                CommandType = ShowLyricsPage {
                    NotSungYetColor = defaultSettings.DefaultTextColor
                    SungColor = defaultSettings.SungTextColor
                    Font = defaultSettings.DefaultFont
                    Lines = [
                        [
                            { Text = "This"; Duration = TimeSpan.FromSeconds(1.) }
                            { Text = " "; Duration = TimeSpan.Zero }
                            { Text = "is"; Duration = TimeSpan.FromSeconds(1.) }
                            { Text = " "; Duration = TimeSpan.Zero }
                            { Text = "page"; Duration = TimeSpan.FromSeconds(3.) }
                        ]
                        [
                            { Text = "#2"; Duration = TimeSpan.FromSeconds(0.2) }
                        ]
                    ]
                }
            }
        ]
        Expect.equal actualCommands expectedCommands "Expected single page with two lines"
]