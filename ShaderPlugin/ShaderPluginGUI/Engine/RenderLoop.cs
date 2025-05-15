using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace ShaderPluginGUI
{
    public delegate void RenderLoopTickDelegate(RenderLoopTickArgs TickEventArgs);

    public class RenderLoop
    {
        public RenderLoopTickDelegate Render;

        volatile bool isRunning = false;
        CancellationTokenSource CancellationTokenSrc;
        Thread RenderThread;
        Dispatcher Dispatcher;

        Stopwatch StopWatch;
        RenderLoopTickArgs TickEvent;
        double[] AverageFrameTimes = new double[60]; // Maximum take 60 frames.
        double targetFPS;

        public RenderLoop(Dispatcher Dispatcher) : this(Dispatcher, 60.0, false)
        {
        }

        public RenderLoop(Dispatcher Dispatcher, double TargetFPS, bool AutoStart = false)
        {
            this.Dispatcher = Dispatcher;
            StopWatch = new Stopwatch();

            this.TargetFPS = TargetFPS; // will set RenderTimer.Interval inside
            TickEvent = new RenderLoopTickArgs(TargetFPS);

            if (AutoStart)
            {
                Start();
            }
        }

        ~RenderLoop()
        {
            Stop();
            Render = null;
            StopWatch = null;
        }

        public void Start(bool ResetTime = false)
        {
            if (ResetTime)
            {
                Reset(false);
            }

            if (!IsRunning)
            {
                isRunning = true;
                CancellationTokenSrc = new CancellationTokenSource();

                RenderThread = new Thread(() => RenderLoopThread(CancellationTokenSrc.Token));
                RenderThread.IsBackground = true;
                RenderThread.Start();
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                CancellationTokenSrc?.Cancel();
                if (RenderThread != null && RenderThread.IsAlive && Thread.CurrentThread != RenderThread)
                {
                    RenderThread.Join();
                }

                RenderThread = null;
            }
        }

        public void Reset(bool Redraw = false)
        {
            bool NeedRestart = IsRunning;
            if (NeedRestart)
            {
                Stop();
            }

            StopWatch.Reset();
            TickEvent = new RenderLoopTickArgs(TargetFPS);

            if (NeedRestart)
            {
                Start(false);
            }
            else if (Redraw)
            {
                Render?.Invoke(TickEvent);
            }
        }

        private void RenderLoopThread(CancellationToken CancelToken)
        {
            try
            {
                AverageFrameTimes = new double[(int)Math.Round(TargetFPS)];

                timeBeginPeriod(1); // Requests a minimum resolution for periodic timers, it's prevent Thread.Sleep(1) spread from 1 to 15.6 ms.

                long StopwatchFrequency = Stopwatch.Frequency;
                StopWatch.Start();

                // Spin wait only if Time smaller then System Timer resolution (1 - 16 ms)
                long SpinThresholdCPUTicks = (long)(StopwatchFrequency * 0.002); // use 2 ms by default or 15.6 ms (if no need to use timeBeginPeriod)

                while (!CancelToken.IsCancellationRequested)
                {
                    long NextTick_CPUTicks = StopWatch.ElapsedTicks + (long)(StopwatchFrequency / TargetFPS);

                    // Fill TickEvent
                    TickEvent.FrameTime = StopWatch.Elapsed.TotalSeconds - TickEvent.RunningTime;
                    TickEvent.RunningTime = StopWatch.Elapsed.TotalSeconds;

                    // Cacl Average FPS
                    int AvgFPSIndex = TickEvent.FrameNumber % AverageFrameTimes.Length;
                    AverageFrameTimes[AvgFPSIndex] = TickEvent.FrameTime;

                    if (AvgFPSIndex == 0) // Recalculate +- every second when buffer fully refreshed
                    {
                        TickEvent.AverageFrameTime = 0.0;
                        int MaxFramesCount = Math.Min(TickEvent.FrameNumber, AverageFrameTimes.Length);
                        for (int i = 0; i < MaxFramesCount; i++)
                        {
                            TickEvent.AverageFrameTime += AverageFrameTimes[i];
                        }
                        TickEvent.AverageFrameTime /= MaxFramesCount;
                    }

                    if (CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (!CancelToken.IsCancellationRequested)
                        {
                            Render?.Invoke(TickEvent); // Render callback
                        }
                    }, DispatcherPriority.Send, CancelToken);

                    TickEvent.FrameNumber = ++TickEvent.FrameNumber % int.MaxValue;

                    // Wait next GameLoop Tick
                    long RemainingCPUTicks = NextTick_CPUTicks - StopWatch.ElapsedTicks;
                    do
                    {
                        if (RemainingCPUTicks <= 0) // FPS <= Target FPS
                        {
                            Thread.Sleep(1); // Reduce big CPU Load
                            break;
                        }

                        if (RemainingCPUTicks > SpinThresholdCPUTicks) // Use Spin instead of Sleep (if Reamaining time < 2 ms)
                        {
                            Thread.Sleep(1); // Low precision sleep for long waits
                        }
                        else
                        {
                            Thread.SpinWait(10); // Spin briefly (CPU-friendly)
                        }

                        RemainingCPUTicks = NextTick_CPUTicks - StopWatch.ElapsedTicks;
                    }
                    while (RemainingCPUTicks > 0 && !CancelToken.IsCancellationRequested);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Reset Timers
                StopWatch.Stop();
                timeEndPeriod(1);

                isRunning = false;
                CancellationTokenSrc?.Dispose();
                CancellationTokenSrc = null;
            }
        }

        public bool IsRunning => isRunning && RenderThread != null;

        public double TargetFPS
        {
            get => targetFPS;
            set
            {
                TickEvent.TargetFPS = targetFPS = Math.Max(value, 1);
            }
        }

        [DllImport("winmm.dll")]
        static extern uint timeBeginPeriod(uint Milliseconds);

        [DllImport("winmm.dll")]
        static extern uint timeEndPeriod(uint Milliseconds);
    }

    public struct RenderLoopTickArgs
    {
        public RenderLoopTickArgs(double TargtFPS)
        {
            this.TargetFPS = TargtFPS;

            FrameNumber = 0;
            RunningTime = 0.0;
            AverageFrameTime = FrameTime = 1.0 / TargtFPS;
        }

        public int FrameNumber;
        public double FrameTime, AverageFrameTime, RunningTime, TargetFPS;

        public double FPS => FrameTime > 0.0 ? 1.0 / FrameTime : TargetFPS;
        public double AverageFPS => AverageFrameTime > 0.0 ? 1.0 / AverageFrameTime : TargetFPS;
    }
}