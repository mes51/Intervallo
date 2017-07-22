using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins.WORLD
{
    public class CheapTrick
    {
        const double DefaultQ1 = -0.15;
        const double DefaultF0Floor = 71.0;
        const double DefaultF0 = 500.0;

        public double Q1 { get; set; }

        public double F0Floor { get; set; }

        public int FFTSize { get; set; }

        private XorShift Rand { get; }

        public CheapTrick(int fs)
        {
            Q1 = DefaultQ1;
            F0Floor = DefaultF0Floor;
            FFTSize = GetFFTSizeForCheapTrick(fs);
            Rand = new XorShift();
        }

        public static double GetF0FloorForCheapTrick(int fs, int fftSize)
        {
            return 3.0 * fs / (fftSize - 3.0);
        }

        public int GetFFTSizeForCheapTrick(int fs)
        {
            return (int)(Math.Pow(2.0, 1.0 + (int)MathUtil.Log2(3.0 * fs / F0Floor + 1.0)));
        }

        public void Estimate(double[] x, int fs, double[] temporalPositions, double[] f0, double[][] spectrogram)
        {
            var f0Floor = GetF0FloorForCheapTrick(fs, FFTSize);
            var spectralEnvelope = new double[FFTSize];

            var forwardRealFFT = ForwardRealFFT.Create(FFTSize);
            var inverseRealFFT = InverseRealFFT.Create(FFTSize);

            for (var i = 0; i < f0.Length; i++)
            {
                var currentF0 = f0[i] <= f0Floor ? DefaultF0 : f0[i];
                CheapTrickGeneralBody(x, fs, currentF0, temporalPositions[i], forwardRealFFT, inverseRealFFT, spectralEnvelope);

                for (int j = 0, limit = FFTSize / 2; j <= limit; j++)
                {
                    spectrogram[i][j] = spectralEnvelope[j];
                }
            }
        }

        //-----------------------------------------------------------------------------
        // CheapTrickGeneralBody() calculates a spectral envelope at a temporal
        // position. This function is only used in CheapTrick().
        // Caution:
        //   forward_fft is allocated in advance to speed up the processing.
        //-----------------------------------------------------------------------------
        void CheapTrickGeneralBody(double[] x, int fs, double currentF0, double currentPosition, ForwardRealFFT forwardRealFFT, InverseRealFFT inverseRealFFT, double[] spectralEnvelope)
        {
            // F0-adaptive windowing
            GetWindowedWaveform(x, fs, currentF0, currentPosition, forwardRealFFT);

            // Calculate power spectrum with DC correction
            // Note: The calculated power spectrum is stored in an array for waveform.
            // In this imprementation, power spectrum is transformed by FFT (NOT IFFT).
            // However, the same result is obtained.
            // This is tricky but important for simple implementation.
            GetPowerSpectrum(fs, currentF0, forwardRealFFT);

            // Smoothing of the power (linear axis)
            // forward_real_fft.waveform is the power spectrum.
            Common.LinearSmoothing(forwardRealFFT.Waveform, currentF0 * 2.0 / 3.0, fs, FFTSize, forwardRealFFT.Waveform);

            // Smoothing (log axis) and spectral recovery on the cepstrum domain.
            SmoothingWithRecovery(currentF0, fs, forwardRealFFT, inverseRealFFT, spectralEnvelope);
        }

        //-----------------------------------------------------------------------------
        // SmoothingWithRecovery() carries out the spectral smoothing and spectral
        // recovery on the Cepstrum domain.
        //-----------------------------------------------------------------------------
        void SmoothingWithRecovery(double f0, int fs, ForwardRealFFT forwardRealFFT, InverseRealFFT inverseRealFFT, double[] spectralEnvelope)
        {
            var smoothingLifter = new double[FFTSize];
            var compensationLifter = new double[FFTSize];

            smoothingLifter[0] = 1;
            compensationLifter[0] = (1.0 - 2.0 * Q1) + 2.0 * Q1;
            for (int i = 1, limit = forwardRealFFT.FFTSize / 2; i <= limit; i++)
            {
                var quefrency = (double)i / fs;
                smoothingLifter[i] = Math.Sin(Math.PI * f0 * quefrency) / (Math.PI * f0 * quefrency);
                compensationLifter[i] = (1.0 - 2.0 * Q1) + 2.0 * Q1 * Math.Cos(2.0 * Math.PI * quefrency * f0);
            }

            for (int i = 0, limit = FFTSize / 2; i <= limit; i++)
            {
                forwardRealFFT.Waveform[i] = Math.Log(forwardRealFFT.Waveform[i]);
            }
            for (int i = 1, limit = FFTSize / 2; i < limit; i++)
            {
                forwardRealFFT.Waveform[FFTSize - i] = forwardRealFFT.Waveform[i];
            }
            FFT.Execute(forwardRealFFT.ForwardFFT);

            for (int i = 0, limit = FFTSize / 2; i <= limit; i++)
            {
                inverseRealFFT.Spectrum[i] = new Complex(forwardRealFFT.Spectrum[i].Real * smoothingLifter[i] * compensationLifter[i] / FFTSize, 0.0);
            }
            FFT.Execute(inverseRealFFT.InverseFFT);

            for (int i = 0, limit = FFTSize / 2; i <= limit; i++)
            {
                spectralEnvelope[i] = Math.Exp(inverseRealFFT.Waveform[i]);
            }
        }

        //-----------------------------------------------------------------------------
        // GetPowerSpectrum() calculates the power_spectrum with DC correction.
        // DC stands for Direct Current. In this case, the component from 0 to F0 Hz
        // is corrected.
        //-----------------------------------------------------------------------------
        void GetPowerSpectrum(int fs, double f0, ForwardRealFFT forwardRealFFT)
        {
            var halfWindowLength = MatlabFunctions.MatlabRound(1.5 * fs / f0);

            // FFT
            Array.Clear(forwardRealFFT.Waveform, halfWindowLength * 2 + 1, FFTSize - halfWindowLength * 2 - 1);
            FFT.Execute(forwardRealFFT.ForwardFFT);

            var powerSpectrum = forwardRealFFT.Waveform;
            var spectrum = forwardRealFFT.Spectrum;
            for (int i = 0, limit = FFTSize / 2; i <= limit; i++)
            {
                powerSpectrum[i] = spectrum[i].Real * spectrum[i].Real + spectrum[i].Imaginary * spectrum[i].Imaginary;
            }

            Common.DCCorrection(powerSpectrum, f0, fs, FFTSize, powerSpectrum);
        }

        //-----------------------------------------------------------------------------
        // GetWindowedWaveform() windows the waveform by F0-adaptive window
        //-----------------------------------------------------------------------------
        void GetWindowedWaveform(double[] x, int fs, double currentF0, double currentPosition, ForwardRealFFT forwardRealFFT)
        {
            var halfWindowLength = MatlabFunctions.MatlabRound(1.5 * fs / currentF0);

            var baseIndex = new int[halfWindowLength * 2 + 1];
            var safeIndex = new int[halfWindowLength * 2 + 1];
            var window = new double[halfWindowLength * 2 + 1];

            SetParametersForGetWindowedWaveform(halfWindowLength, x.Length, currentPosition, fs, currentF0, baseIndex, safeIndex, window);

            // F0-adaptive windowing
            var waveForm = forwardRealFFT.Waveform;
            for (int i = 0, limit = halfWindowLength * 2; i <= limit; i++)
            {
                waveForm[i] = x[safeIndex[i]] * window[i] + Rand.Next() * 0.000000000000001;
            }

            var tmpWeight1 = waveForm.Take(halfWindowLength * 2 + 1).Sum();
            var tmpWeight2 = window.Sum();
            var weightingCoefficient = tmpWeight1 / tmpWeight2;
            for (int i = 0, limit = halfWindowLength * 2; i <= limit; i++)
            {
                waveForm[i] -= window[i] * weightingCoefficient;
            }
        }

        //-----------------------------------------------------------------------------
        // SetParametersForGetWindowedWaveform()
        //-----------------------------------------------------------------------------
        void SetParametersForGetWindowedWaveform(int halfWindowLength, int xLength, double currentPosition, int fs, double currentF0, int[] baseIndex, int[] safeIndex, double[] window)
        {
            for (var i = -halfWindowLength; i <= halfWindowLength; i++)
            {
                baseIndex[i + halfWindowLength] = i;
            }
            var origin = MatlabFunctions.MatlabRound(currentPosition * fs + 0.001);
            for (int i = 0, limit = halfWindowLength * 2; i <= limit; i++)
            {
                safeIndex[i] = Math.Min(xLength - 1, Math.Max(0, origin + baseIndex[i]));
            }

            var average = 0.0;
            for (int i = 0, limit = halfWindowLength * 2; i <= limit; i++)
            {
                var position = baseIndex[i] / 1.5 / fs;
                window[i] = 0.5 * Math.Cos(Math.PI * position * currentF0) + 0.5;
                average += window[i] * window[i];
            }
            average = Math.Sqrt(average);
            for (int i = 0, limit = halfWindowLength * 2; i <= limit; i++)
            {
                window[i] /= average;
            }
        }
    }
}
