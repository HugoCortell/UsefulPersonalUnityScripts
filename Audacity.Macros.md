# Audacity Macros
My personal list of audacity macros.

## Robot Voice
### Main Track (Apply this to the original track, then copy the result to the new track)
```
BassAndTreble:Bass="15" Gain="1" Link_Sliders="0" Treble="0"
NewStereoTrack:
```

## Track 1 (Apply to top track, mess with first value to get different results)
```
ChangePitch:Percentage="-10" SBSMS="1"
```

## Track 2 (Apply to the other track, first value should always be half of the top)
```
ChangePitch:Percentage="-5" SBSMS="1"
Phaser:Depth="255" DryWet="255" Feedback="0" Freq="0.1" Gain="0" Phase="0" Stages="24"
```
