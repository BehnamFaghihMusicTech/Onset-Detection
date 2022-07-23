using System;
using System.Collections.Generic;
using System.Linq;

namespace OnsetDetector
{
    public class DetectedPitch
    {
        public PitchFrame Pitch;
        public double StretchedPitch;
        public bool? Onset;
        public bool? Offset;
        public bool Silent;
        public double PitchSlope;
        public double FollowingPitchSlope;
        public double PercentOffsetBasedPitch;
        public double PercentOnsetBasedPitch;
        public double AvgFollowingPitchSlope;
        public double StdFollowingPitchSlope;
        public bool TransitionNote;

        public static List<DetectedPitch> StretchingPitch(List<DetectedPitch> detectedPitches, double maxPitchThreshold)
        {
            List<DetectedPitch> pitches = detectedPitches;
            double maxPitch = Convert.ToDouble(pitches.Max(t => t.Pitch.F0Hz));
            double coefficient = maxPitchThreshold / maxPitch;
            for (int i = 0; i < detectedPitches.Count; i++)
            {
                pitches[i].StretchedPitch = Convert.ToDouble(pitches[i].Pitch.F0Hz) * coefficient;
            }
            return pitches;
        }

        public static List<DetectedPitch> AddSlopes(List<DetectedPitch> detectedPitches, double timeDistanceBetweenFrames)
        {
            List<DetectedPitch> pitches = detectedPitches;
            for (int i = 1; i < detectedPitches.Count; i++)
            {
                double timeDiff = 0;
                if (detectedPitches[i].Pitch.TimeSecond != null && detectedPitches[i - 1].Pitch.TimeSecond != null)
                    timeDiff = Convert.ToDouble(detectedPitches[i].Pitch.TimeSecond) -
                               Convert.ToDouble(detectedPitches[i - 1].Pitch.TimeSecond);

                timeDiff = timeDiff > 0 ? timeDiff : timeDistanceBetweenFrames;
                
                double? diff = detectedPitches[i].StretchedPitch - detectedPitches[i - 1].StretchedPitch;
                double slope = Convert.ToDouble(diff) / timeDiff;
                pitches[i].PitchSlope = slope;
            }
            return pitches;
        }

        public static List<DetectedPitch> AddFollowingSlopes(List<DetectedPitch> detectedPitches)
        {
            List<DetectedPitch> pitches = detectedPitches;
            for (int i = 0; i < detectedPitches.Count; i++)
            {
                double sumSlope = 0;
                sumSlope = pitches[i].PitchSlope;
                for (int j = i + 1; j < detectedPitches.Count; j++)
                {
                    if ((pitches[i].PitchSlope > 0 && pitches[j].PitchSlope > 0) || (pitches[i].PitchSlope < 0 && pitches[j].PitchSlope < 0))
                        sumSlope += pitches[j].PitchSlope;
                    else
                        break;
                }
                pitches[i].FollowingPitchSlope = sumSlope;
            }
            return pitches;
        }

        public static List<DetectedPitch> AddAvgStdFollowingPitchSlopesInPercent(List<DetectedPitch> detectedPitches, int noBefore, int noAfter=0)
        {
            List<DetectedPitch> pitches = detectedPitches;
            for (int i = noBefore; i < pitches.Count-noAfter; i++)
            {
                List<double> localPitches =
                    pitches.Skip(i - noBefore).Select(t => t.FollowingPitchSlope).Take(noBefore + noAfter).ToList();
                double avg = localPitches.Average();
                double std = MathFunctions.StandardDeviation(localPitches, avg, true);
                pitches[i].AvgFollowingPitchSlope = avg;
                pitches[i].StdFollowingPitchSlope = std;
            }
            return pitches;
        }
    }
}
