using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins.WORLD
{
    public class Synthesis
    {
        const double DefaultF0 = 500.0;
        const double SafeGuardMinimum = 0.000000000001;

        private XorShift Rand { get; }

        public Synthesis()
        {
            Rand = new XorShift();
        }

        public void Synthesize(double[] f0, int f0Length, double[][] spectrogram, double[][] aperiodicity, int fftSize, double framePeriod, int fs, double[] y)
        {
            var minimumPhase = MinimumPhaseAnalysis.Create(fftSize);
            var inverseRealFFT = InverseRealFFT.Create(fftSize);
            var forwardRealFFT = ForwardRealFFT.Create(fftSize);

            var pulseLocations = new double[y.Length];
            var pulseLocationsIndex = new int[y.Length];
            var interpolatedVUV = new double[y.Length];
            var numberOfPulses = GetTimeBase(f0, f0Length, fs, framePeriod / 1000.0, y.Length, pulseLocations, pulseLocationsIndex, interpolatedVUV);

            var dcRemover = GetDCRemover(fftSize);

            framePeriod /= 1000.0;

            var impulseResponse = new double[fftSize];
            for (var i = 0; i < numberOfPulses; i++)
            {
                var noiseSize = pulseLocationsIndex[Math.Min(numberOfPulses - 1, i + 1)] - pulseLocationsIndex[i];

                GetOneFrameSegment(interpolatedVUV[pulseLocationsIndex[i]], noiseSize, spectrogram, fftSize, aperiodicity, f0Length, framePeriod, pulseLocations[i], fs, forwardRealFFT, inverseRealFFT, minimumPhase, dcRemover, impulseResponse);

                for (var j = 0; j < fftSize; j++)
                {
                    var safeIndex = Math.Min(y.Length - 1, Math.Max(0, j + pulseLocationsIndex[i] - fftSize / 2 + 1));
                    y[safeIndex] += impulseResponse[j];
                }
            }
        }

        //-----------------------------------------------------------------------------
        // GetOneFrameSegment() calculates a periodic and aperiodic response at a time.
        //-----------------------------------------------------------------------------
        void GetOneFrameSegment(double currentVUV, int noiseSize, double[][] spectrogram, int fftSize, double[][] aperiodicity, int f0Length, double framePeriod, double currentTime, int fs, ForwardRealFFT forwardRealFFT, InverseRealFFT inverseRealFFT, MinimumPhaseAnalysis minimumPhase, double[] dcRemover, double[] response)
        {
            var aperiodicResponse = new double[fftSize];
            var periodicResponse = new double[fftSize];

            var spectralEnvelope = new double[fftSize];
            var aperiodicRatio = new double[fftSize];
            GetSpectralEnvelope(currentTime, framePeriod, f0Length, spectrogram, fftSize, spectralEnvelope);
            GetAperiodicRatio(currentTime, framePeriod, f0Length, aperiodicity, fftSize, aperiodicRatio);

            // Synthesis of the periodic response
            GetPeriodicResponse(fftSize, spectralEnvelope, aperiodicRatio, currentVUV, inverseRealFFT, minimumPhase, dcRemover, periodicResponse);

            // Synthesis of the aperiodic response
            GetAperiodicResponse(noiseSize, fftSize, spectralEnvelope, aperiodicRatio, currentVUV, forwardRealFFT, inverseRealFFT, minimumPhase, aperiodicResponse);

            var sqrtNoiseSize = Math.Sqrt(noiseSize);
            for (var i = 0; i < fftSize; i++)
            {
                response[i] = (periodicResponse[i] * sqrtNoiseSize + aperiodicResponse[i]) / fftSize;
            }
        }

        //-----------------------------------------------------------------------------
        // GetAperiodicResponse() calculates an aperiodic response.
        //-----------------------------------------------------------------------------
        void GetAperiodicResponse(int noiseSize, int fftSize, double[] spectrum, double[] aperiodicRatio, double currentVUV, ForwardRealFFT forwardRealFFT, InverseRealFFT inverseRealFFT, MinimumPhaseAnalysis minimumPhase, double[] aperiodicResponse)
        {
            GetNoiseSpectrum(noiseSize, fftSize, forwardRealFFT);

            var logSpectrum = minimumPhase.LogSpectrum;
            if (currentVUV != 0.0)
            {
                for (int i = 0, limit = minimumPhase.FFTSize / 2; i <= limit; i++)
                {
                    logSpectrum[i] = Math.Log(spectrum[i] * aperiodicRatio[i]) / 2.0;
                }
            }
            else
            {
                for (int i = 0, limit = minimumPhase.FFTSize / 2; i <= limit; i++)
                {
                    logSpectrum[i] = Math.Log(spectrum[i]) / 2.0;
                }
            }
            Common.GetMinimumPhaseSpectrum(minimumPhase);

            var minimumPhaseSpectrum = minimumPhase.MinimumPhaseSpectrum;
            Array.Copy(minimumPhaseSpectrum, 0, inverseRealFFT.Spectrum, 0, fftSize / 2 + 1);
            for (int i = 0, limit = fftSize / 2; i <= limit; i++)
            {
                var real = minimumPhaseSpectrum[i].Real * forwardRealFFT.Spectrum[i].Real - minimumPhaseSpectrum[i].Imaginary * forwardRealFFT.Spectrum[i].Imaginary;
                var imaginary = minimumPhaseSpectrum[i].Real * forwardRealFFT.Spectrum[i].Imaginary + minimumPhaseSpectrum[i].Imaginary * forwardRealFFT.Spectrum[i].Real;

                inverseRealFFT.Spectrum[i] = new Complex(real, imaginary);
            }
            FFT.Execute(inverseRealFFT.InverseFFT);
            MatlabFunctions.FFTShift(inverseRealFFT.Waveform.SubSequence(0, fftSize), aperiodicResponse);
        }

        void GetNoiseSpectrum(int noiseSize, int fftSize, ForwardRealFFT forwardRealFFT)
        {
            var waveform = forwardRealFFT.Waveform;
            for (var i = 0; i < noiseSize; i++)
            {
                waveform[i] = Rand.Next();
            }

            var average = noiseSize > 0 ? waveform.Take(noiseSize).Average() : 1.0;
            for (var i = 0; i < noiseSize; i++)
            {
                waveform[i] -= average;
            }
            Array.Clear(waveform, noiseSize, fftSize - noiseSize);
            FFT.Execute(forwardRealFFT.ForwardFFT);
        }

        //-----------------------------------------------------------------------------
        // GetPeriodicResponse() calculates an aperiodic response.
        //-----------------------------------------------------------------------------
        void GetPeriodicResponse(int fftSize, double[] spectrum, double[] aperiodicRatio, double currentVUV, InverseRealFFT inverseRealFFT, MinimumPhaseAnalysis minimumPhase, double[] dcRemover, double[] periodicResponse)
        {
            if (currentVUV <= 0.5 || aperiodicRatio[0] > 0.999)
            {
                Array.Clear(periodicResponse, 0, fftSize);
                return;
            }

            var logSpectrum = minimumPhase.LogSpectrum;
            for (int i = 0, limit = minimumPhase.FFTSize / 2; i <= limit; i++)
            {
                logSpectrum[i] = Math.Log(spectrum[i] * (1.0 - aperiodicRatio[i]) + SafeGuardMinimum) / 2.0;
            }
            Common.GetMinimumPhaseSpectrum(minimumPhase);

            Array.Copy(minimumPhase.MinimumPhaseSpectrum, 0, inverseRealFFT.Spectrum, 0, fftSize / 2 + 1);
            FFT.Execute(inverseRealFFT.InverseFFT);
            MatlabFunctions.FFTShift(inverseRealFFT.Waveform.SubSequence(0, fftSize), periodicResponse);
            RemoveDCComponent(periodicResponse, fftSize, dcRemover, periodicResponse);
        }

        //-----------------------------------------------------------------------------
        // RemoveDCComponent()
        //-----------------------------------------------------------------------------
        void RemoveDCComponent(double[] periodicResponse, int fftSize, double[] dcRemover, double[] newPeriodicResponse)
        {
            var dcComponent = periodicResponse.Take(fftSize).Skip(fftSize / 2).Sum();
            for (int i = 0, limit = fftSize / 2; i < limit; i++)
            {
                newPeriodicResponse[i] = -dcComponent * dcRemover[i];
            }
            for (var i = fftSize / 2; i < fftSize; i++)
            {
                newPeriodicResponse[i] -= dcComponent * dcRemover[i];
            }
        }

        void GetAperiodicRatio(double currentTime, double framePeriod, int f0Length, double[][] aperiodicity, int fftSize, double[] aperiodicSpectrum)
        {
            var currentFrameFloor = Math.Min(f0Length - 1, (int)Math.Floor(currentTime / framePeriod));
            var currentFrameCeil = Math.Min(f0Length - 1, (int)Math.Ceiling(currentTime / framePeriod));
            var interpolation = currentTime / framePeriod - currentFrameFloor;

            if (currentFrameFloor == currentFrameCeil)
            {
                for (int i = 0, limit = fftSize / 2; i <= limit; i++)
                {
                    aperiodicSpectrum[i] = Math.Pow(Common.GetSafeAperiodicity(aperiodicity[currentFrameFloor][i]), 2.0);
                }
            }
            else
            {
                for (int i = 0, limit = fftSize / 2; i <= limit; i++)
                {
                    aperiodicSpectrum[i] = Math.Pow((1.0 - interpolation) * Common.GetSafeAperiodicity(aperiodicity[currentFrameFloor][i]) + interpolation * Common.GetSafeAperiodicity(aperiodicity[currentFrameCeil][i]), 2.0);
                }
            }
        }

        void GetSpectralEnvelope(double currentTime, double framePeriod, int f0Length, double[][] spectrogram, int fftSize, double[] spectralEnvelope)
        {
            int currentFrameFloor = Math.Min(f0Length - 1, (int)Math.Floor(currentTime / framePeriod));
            int currentFrameCeil = Math.Min(f0Length - 1, (int)Math.Ceiling(currentTime / framePeriod));
            double interpolation = currentTime / framePeriod - currentFrameFloor;

            if (currentFrameFloor == currentFrameCeil)
            {
                for (int i = 0, limit = fftSize / 2; i <= limit; i++)
                {
                    spectralEnvelope[i] = Math.Abs(spectrogram[currentFrameFloor][i]);
                }
            }
            else
            {
                for (int i = 0, limit = fftSize / 2; i <= limit; i++)
                {
                    spectralEnvelope[i] = (1.0 - interpolation) * Math.Abs(spectrogram[currentFrameFloor][i]) + interpolation * Math.Abs(spectrogram[currentFrameCeil][i]);
                }
            }
        }

        double[] GetDCRemover(int fftSize)
        {
            var dcRemover = new double[fftSize];
            var dcComponent = 0.0;
            for (int i = 0, limit = fftSize / 2; i < limit; i++)
            {
                dcRemover[i] = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * (i + 1.0) / (1.0 + fftSize));
                dcRemover[fftSize - i - 1] = dcRemover[i];
                dcComponent += dcRemover[i] * 2.0;
            }
            for (int i = 0, limit = fftSize / 2; i < limit; i++)
            {
                dcRemover[i] /= dcComponent;
                dcRemover[fftSize - i - 1] = dcRemover[i];
            }

            return dcRemover;
        }

        int GetTimeBase(double[] f0, int f0Length, int fs, double framePeriod, int yLength, double[] pulseLocations, int[] pulseLocationsIndex, double[] interpolatedVUV)
        {
            var timeAxis = new double[yLength];
            var coarseTimeAxis = new double[f0Length + 1];
            var coarseF0 = new double[f0Length + 1];
            var coarseVUV = new double[f0Length + 1];
            GetTemporalParametersForTimeBase(f0, f0Length, fs, yLength, framePeriod, timeAxis, coarseTimeAxis, coarseF0, coarseVUV);

            var interpolatedF0 = new double[yLength];
            MatlabFunctions.Interp1(coarseTimeAxis, coarseF0, timeAxis, interpolatedF0);
            MatlabFunctions.Interp1(coarseTimeAxis, coarseVUV, timeAxis, interpolatedVUV);
            for (var i = 0; i < yLength; i++)
            {
                interpolatedVUV[i] = interpolatedVUV[i] > 0.5 ? 1.0 : 0.0;
                interpolatedF0[i] = interpolatedVUV[i] == 0.0 ? DefaultF0 : interpolatedF0[i];
            }

            return GetPulseLocationsForTimeBase(interpolatedF0, timeAxis, yLength, fs, pulseLocations, pulseLocationsIndex);
        }

        int GetPulseLocationsForTimeBase(double[] interpolatedF0, double[] timeAxis, int yLength, int fs, double[] pulseLocations, int[] pulseLocationsIndex)
        {
            var totalPhase = interpolatedF0.Take(yLength).Select((x) => 2.0 * Math.PI * x / fs).ToArray();
            for (var i = 1; i < totalPhase.Length; i++)
            {
                totalPhase[i] += totalPhase[i - 1];
            }

            var warpPhase = totalPhase.Select((x) => x % (2.0 * Math.PI)).ToArray();

            var warpPhaseABS = new double[yLength];
            for (int i = 0, limit = yLength - 1; i < limit; i++)
            {
                warpPhaseABS[i] = Math.Abs(warpPhase[i + 1] - warpPhase[i]);
            }

            var numberOfPulses = 0;
            for (int i = 0, limit = yLength - 1; i < limit; i++)
            {
                if (warpPhaseABS[i] > Math.PI)
                {
                    pulseLocations[numberOfPulses] = timeAxis[i];
                    pulseLocationsIndex[numberOfPulses] = MatlabFunctions.MatlabRound(pulseLocations[numberOfPulses] * fs);
                    numberOfPulses++;
                }
            }

            return numberOfPulses;
        }

        void GetTemporalParametersForTimeBase(double[] f0, int f0Length, int fs, int yLength, double framePeriod, double[] timeAxis, double[] coarseTimeAxis, double[] coarseF0, double[] coarseVUV)
        {
            for (var i = 0; i < yLength; i++)
            {
                timeAxis[i] = i / (double)fs;
            }

            // the array 'coarse_time_axis' is supposed to have 'f0_length + 1' positions
            for (var i = 0; i <= f0Length; i++)
            {
                coarseTimeAxis[i] = i * framePeriod;
            }
            f0.BlockCopy(0, coarseF0, 0, f0Length);
            coarseF0[f0Length] = coarseF0[f0Length - 1] * 2.0 - coarseF0[f0Length - 2];
            for (var i = 0; i < f0Length; i++)
            {
                coarseVUV[i] = f0[i] == 0.0 ? 0.0 : 1.0;
            }
            coarseVUV[f0Length] = coarseVUV[f0Length - 1] * 2.0 - coarseVUV[f0Length - 2];
        }
    }
}
