# Showlight Generator CLI

## Use

```
-f, --FogSourceArrangement     Required. Sets the file from which fog colors are generated.

--FogMethod                    (Default: bars) Sets the method used for fog generation (Options: bars, sections,
                               octave, chords, time).

--FogBars                      (Default: 16) Sets the minimum number of measures between fog note changes when using
                               the 'bars' method.

--FogTime                      (Default: 5) Sets the minimum time in seconds between fog note changes when using the
                               'time' method.

-b, --BeamSourceArrangement    (Default: Same as fog) Sets the file from which beam colors are generated.

--BeamMethod                   (Default: time) Sets the method used for beam generation (Options: time, fog).

--BeamTime                     (Default: 0.5) Sets the minimum time in seconds between beam note changes when using
                               the 'time' method.

-o, --Output                   (Default: './showlights.xml') Sets the path for the XML file that will be generated.

-l, --EnableLasers             (Default: true) Enables laser lights.

-c, --UseCompatibleColors      (Default: false) When enabled, color combinations that may look bad are avoided.

-r, --Randomize                Enables or disables randomization.

-v, --Verbose                  (Default: false) If enabled, prints the selected options to the console.

--help                         Display this help screen.

--version                      Display version information.
```

## Examples

`ShowLightGeneratorCLI.exe -f ".\PART_LEAD.xml" --FogMethod sections -r beam fog -c`

Generate the fog notes from the sections, randomize beam and fog notes and use compatible colors.

`ShowLightGeneratorCLI.exe -f ".\PART_LEAD.xml" --BeamMethod fog --EnableLasers false`

Generate the beam notes to match the generated fog notes and disable the laser lights.

## Verbose Mode

Enabling verbose mode prints a summary of the enabled options, for example:

```
FOG
Source: some_file.xml
Method: FromChords
Randomized: True
--------------------------------------------
BEAMS
Source: some_file.xml
Method: MinTimeBetweenChanges
Minimum time: 0.5 seconds
Use compatible colors: True
Randomized: True
--------------------------------------------
LASERS
Enabled: True
--------------------------------------------
Output path: .\showlights.xml
File generated successfully.
```
