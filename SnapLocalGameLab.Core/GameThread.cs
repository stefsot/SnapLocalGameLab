using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IL2CppApi.Runtime;

namespace SnapLocalGameLab.Core
{
    internal static class GameThread
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void UpdateDelegate(IntPtr self, IntPtr method);
        private static UpdateDelegate _original;
        private static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();

        public static void Initialize()
        {
            if (_original != null) return;
            using (var runtime = new Il2CppRuntime())
            {
                var type = runtime.Class("App.View.dll", "CubeUnity.App.View", "UiVfxManager");
                var update = runtime.Method(type, "OnUpdate", false);
                _original = HookManager.CreateHook<UpdateDelegate>(update.MethodPointer, Update, "Local game dispatcher");
            }
        }

        public static async Task<T> Run<T>(Func<T> action)
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            int state = 0;
            Queue.Enqueue(() =>
            {
                if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;
                try { completion.TrySetResult(action()); }
                catch (Exception e) { completion.TrySetException(e); }
            });
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)).ConfigureAwait(false) != completion.Task
                && Interlocked.CompareExchange(ref state, 2, 0) == 0)
                completion.TrySetException(new TimeoutException("The game update did not respond. Wait for the home screen and retry."));
            return await completion.Task.ConfigureAwait(false);
        }

        private static void Update(IntPtr self, IntPtr method)
        {
            if (Queue.TryDequeue(out var action)) action();
            _original(self, method);
        }
    }
}
