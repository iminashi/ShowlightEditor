using ReactiveUI;
using ShowlightEditor.Core.Models;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using XmlUtils;

namespace ShowlightEditor.Core
{
    public sealed class SuspensionDriver : ISuspensionDriver
    {
        private const string PrefFileName = "GenerationPreferences.xml";
        private readonly string fullpathFilename;

        public SuspensionDriver()
        {
            fullpathFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PrefFileName);
        }

        public IObservable<Unit> InvalidateState()
        {
            return Observable.Create<Unit>(observer =>
            {
                try
                {
                    File.Delete(fullpathFilename);
                }
                catch
                {
                }

                observer.OnNext(Unit.Default);
                observer.OnCompleted();

                return Disposable.Empty;
            });
        }

        public IObservable<object> LoadState()
        {
            return Observable.Create<object>(observer =>
            {
                if (!File.Exists(fullpathFilename))
                {
                    observer.OnError(new Exception("Config file not found."));
                    return Disposable.Empty;
                }

                try
                {
                    var config = new GenerationPreferences();
                    ReflectionConfig.LoadFromXml(fullpathFilename, config);
                    observer.OnNext(config);
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return Disposable.Empty;
            });
        }

        public IObservable<Unit> SaveState(object state)
        {
            return Observable.Create<Unit>(observer =>
            {
                try
                {
                    ReflectionConfig.SaveToXml(fullpathFilename, state);
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return Disposable.Empty;
            });
        }
    }
}
