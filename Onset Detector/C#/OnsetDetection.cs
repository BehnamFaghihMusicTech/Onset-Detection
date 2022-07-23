using System;
using System.Collections.Generic;


namespace OnsetDetector
{
    class OnsetDetection
    {
        private OnsetSettings _onsetSettings;
        private List<DetectedPitch> _detectedPitches;
        private double _timeDistanceBetweenFrames;

        public OnsetDetection(List<DetectedPitch> detectedPitches, OnsetSettings onsetSettings,
            double timeDistanceBetweenFrames)
        {
            InitialVariables(detectedPitches, onsetSettings, timeDistanceBetweenFrames);
        }

        private void InitialVariables(List<DetectedPitch> detectedPitches, OnsetSettings onsetSettings, double timeDistanceBetweenFrames)
        {
            _detectedPitches = detectedPitches;
            _onsetSettings = onsetSettings;
            _timeDistanceBetweenFrames = timeDistanceBetweenFrames;
        }

        public List<DetectedPitch> GetAllOnsets()
        {
            CalculateOnset();
            return _detectedPitches;
        }

        private void CalculateOnset()
        {
            _detectedPitches = DetectedPitch.StretchingPitch(_detectedPitches, _onsetSettings.MaxPitchThreshold);
            _detectedPitches = DetectedPitch.AddSlopes(_detectedPitches, _timeDistanceBetweenFrames);
            _detectedPitches = DetectedPitch.AddFollowingSlopes(_detectedPitches);
            _detectedPitches = DetectedPitch.AddAvgStdFollowingPitchSlopesInPercent(_detectedPitches, _onsetSettings.NumberOfF0sBefore, _onsetSettings.NumberOfF0sAfter);
            _detectedPitches = FillOnsetPercentages(_detectedPitches);

            for (int i = 0; i < _detectedPitches.Count; i++)
            {
                if (_detectedPitches[i].PercentOnsetBasedPitch >= 100)
                {
                    _detectedPitches[i].Onset = true;
                    if (i >= 1)
                        _detectedPitches[i - 1].Offset = true;
                    i += Convert.ToInt32(_onsetSettings.MinNumberOfFrequenciesBetweenOnsets);
                }
            }

            _detectedPitches = AlteringDetectedOnsets(_detectedPitches,
                _onsetSettings);
            _detectedPitches = AddingTransitions(_detectedPitches);
        }

        private List<DetectedPitch> AddingTransitions(List<DetectedPitch> detectedPitches)
        {
            List<DetectedPitch> pitches = detectedPitches;
            for(int i=0;i<pitches.Count;i++)
            {
                if(pitches[i].Onset==true && pitches[i].TransitionNote==false)
                {                    
                    bool positive1 = pitches[i].FollowingPitchSlope > 0 ? true : false;
                    int j = i + 1;
                    for (;j<pitches.Count;j++)
                    {
                        bool positive2 = pitches[j].FollowingPitchSlope > 0 ? true : false;
                        if ((positive1 != positive2 || (Math.Abs(pitches[j].FollowingPitchSlope)<0.2)) && j-i>1)
                        {
                            pitches[j].Onset = true;
                            pitches[j - 1].Offset = true;
                            pitches[i].TransitionNote = true;
                            pitches[j - 1].TransitionNote = true;
                            break;
                        }
                    }
                    i = j;
                }
            }
            return pitches;
        }

        private List<DetectedPitch> FillOnsetPercentages(List<DetectedPitch> pitches)
        {
            List<DetectedPitch> detectedPitches = pitches;

            for (int i = 0; i < detectedPitches.Count - 1; i++)
            {
                if (pitches[i].Silent == false && pitches[i + 1].Silent == true)
                {
                    detectedPitches[i + 1].PercentOnsetBasedPitch = 100;
                    detectedPitches[i + 1].PercentOffsetBasedPitch = 0;

                    detectedPitches[i].PercentOnsetBasedPitch = 0;
                    detectedPitches[i].PercentOffsetBasedPitch = 100;
                    i++;
                }
                else if (pitches[i].Silent == true && pitches[i + 1].Silent == false)
                {
                    detectedPitches[i].PercentOnsetBasedPitch = 0;
                    detectedPitches[i].PercentOffsetBasedPitch = 100;

                    detectedPitches[i + 1].PercentOnsetBasedPitch = 100;
                    detectedPitches[i + 1].PercentOffsetBasedPitch = 0;
                    i++;
                }
                else if (pitches[i].Silent == true && pitches[i + 1].Silent == true)
                {
                    detectedPitches[i + 1].PercentOnsetBasedPitch = 0;
                    detectedPitches[i + 1].PercentOffsetBasedPitch = 0;
                }
                else
                {
                    if (i >= 1 &&
                        (detectedPitches[i - 1].StdFollowingPitchSlope > 0.0000001 ||
                         detectedPitches[i - 1].StdFollowingPitchSlope < -0.0000001) &&
                        Math.Abs(detectedPitches[i].FollowingPitchSlope) >
                        Math.Abs(detectedPitches[i - 1].AvgFollowingPitchSlope +
                                 (detectedPitches[i - 1].StdFollowingPitchSlope *
                                  _onsetSettings.PitchThreshold)))
                    {
                        detectedPitches[i].PercentOnsetBasedPitch = 100;
                    }
                }
            }

            return detectedPitches;
        }

        public List<DetectedPitch> AlteringDetectedOnsets(List<DetectedPitch> pitches, OnsetSettings onsetSettings)
        {
            List<DetectedPitch> detectedPitches = pitches;
            double maxDiff;
            int indexMaxDiff;
            double tempDiff;
            for (int i = 0; i < detectedPitches.Count; i++)
            {
                if (detectedPitches[i].Onset == true && i >= 1)
                {
                    maxDiff = Math.Abs(detectedPitches[i].FollowingPitchSlope) -
                              Math.Abs((detectedPitches[i - 1].AvgFollowingPitchSlope +
                                        (detectedPitches[i - 1].StdFollowingPitchSlope *
                                         onsetSettings.PitchThreshold)));
                    indexMaxDiff = i;
                    int j;
                    for (j = i + 1;
                        j <= i + onsetSettings.MinNumberOfFrequenciesBetweenOnsets && j < detectedPitches.Count;
                        j++)
                    {
                        if (detectedPitches[j].Silent == true)
                        {
                            break;
                        }

                        if (detectedPitches[j].Onset == true)
                        {
                            tempDiff = Math.Abs(detectedPitches[j].FollowingPitchSlope) -
                                       Math.Abs((detectedPitches[j - 1].AvgFollowingPitchSlope +
                                                 (detectedPitches[j - 1].StdFollowingPitchSlope *
                                                  onsetSettings.PitchThreshold)));
                            if (tempDiff > maxDiff)
                            {
                                detectedPitches[indexMaxDiff].Onset = false;
                                detectedPitches[indexMaxDiff - 1].Offset = false;
                                maxDiff = tempDiff;
                                indexMaxDiff = j;
                            }
                            else
                            {
                                detectedPitches[j].Onset = false;
                                if (detectedPitches[j - 1].Offset == true)
                                {
                                    detectedPitches[j - 1].Offset = false;
                                }
                            }
                        }
                    }

                    i = j - 1;
                    if (i >= detectedPitches.Count)
                        break;

                    i += onsetSettings.MinNumberOfFrequenciesBetweenOnsets - 1;
                }
            }

            return detectedPitches;
        }
    }
}
