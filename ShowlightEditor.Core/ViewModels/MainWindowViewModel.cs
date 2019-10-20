using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ShowlightEditor.Core.Models;
using ShowlightEditor.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using XmlUtils;

namespace ShowlightEditor.Core.ViewModels
{
    public sealed class MainWindowViewModel : ReactiveObject
    {
        public const string ProgramName = "Showlight Editor";

        public ReadOnlyObservableCollection<Showlight> ObservableShowlights { get; }
        public UndoManager UndoManager { get; }

        private SourceCache<Showlight, int> Showlights { get; } = new SourceCache<Showlight, int>(sl => sl.Id);

        private readonly IPlatformSpecificServices services;

        public GenerationViewModel GenerationVM { get; }
        public TimeShifterViewModel TimeShiftVM { get; }
        public ReplaceViewModel ReplaceVM { get; }
        public StrobeEffectViewModel StrobeEffectVM { get; }
        public LaserLightsViewModel LaserLightsVM { get; }


        public IObservable<Showlight> ScrollIntoView => scrollIntoView;
        private readonly Subject<Showlight> scrollIntoView = new Subject<Showlight>();

        [Reactive]
        public string OpenFileName { get; set; }

        [Reactive]
        public bool FileDirty { get; set; }

        [Reactive]
        public bool EditorEnabled { get; private set; } = true;

        [Reactive]
        public string WindowTitle { get; set; } = ProgramName;

        [Reactive]
        public int ActiveFogColor { get; private set; }

        [Reactive]
        public int ActiveBeamColor { get; private set; }

        public extern string PreviewTooltip { [ObservableAsProperty]get; }

        [Reactive]
        public Showlight SelectedItem { get; set; }

        public IList SelectedItems { get; set; }

        public int InsertColor { get; set; }

        [Reactive]
        public float InsertTime { get; set; }

        [Reactive]
        public float MoveToTime { get; set; }

        #region Reactive Commands

        public ReactiveCommand<Unit, Unit> NewFile { get; private set; }
        public ReactiveCommand<Unit, Unit> OpenFile { get; private set; }
        public ReactiveCommand<Unit, Unit> SaveFile { get; private set; }
        public ReactiveCommand<Unit, Unit> SaveFileAs { get; private set; }

        public ReactiveCommand<Unit, Unit> Insert { get; private set; }
        public ReactiveCommand<Unit, Unit> Move { get; private set; }
        public ReactiveCommand<Unit, Unit> Cut { get; private set; }
        public ReactiveCommand<Unit, Unit> Copy { get; private set; }
        //public ReactiveCommand<Unit, Unit> PasteReplace { get; private set; }
        //public ReactiveCommand<Unit, Unit> PasteInsert { get; private set; }
        public ReactiveCommand<PasteType, Unit> Paste { get; private set; }
        public ReactiveCommand<Unit, Unit> Undo { get; private set; }
        public ReactiveCommand<Unit, Unit> Redo { get; private set; }
        public ReactiveCommand<Unit, Unit> Delete { get; private set; }

        public ReactiveCommand<ShowlightType, Unit> DeleteAll { get; private set; }
        public ReactiveCommand<Unit, Unit> OptimizeShowlights { get; private set; }
        public ReactiveCommand<Unit, Unit> SetLaserLights { get; private set; }
        public ReactiveCommand<Unit, Unit> FindLasersOn { get; private set; }

        public ReactiveCommand<int, Unit> ColorSelect { get; private set; }

        public ReactiveCommand<Unit, Unit> Generate { get; private set; }
        public ReactiveCommand<Unit, Unit> ShiftTimes { get; private set; }
        public ReactiveCommand<Unit, Unit> Replace { get; private set; }
        public ReactiveCommand<Unit, Unit> StrobeEffect { get; private set; }

        #endregion

#if DEBUG
        // Design time data
        public MainWindowViewModel() : this(null)
        {
            var sl1 = new Showlight(Showlight.FogMax, 10f);
            var sl2 = new Showlight(Showlight.BeamMin, 11f);
            var sl3 = new Showlight(Showlight.LasersOn, 12f);
            Showlights.AddOrUpdate(sl1);
            Showlights.AddOrUpdate(sl2);
            Showlights.AddOrUpdate(sl3);

            ActiveFogColor = sl1.Note;
            ActiveBeamColor = sl2.Note;
        }
#endif

        public MainWindowViewModel(IPlatformSpecificServices services)
        {
            this.services = services;
            GenerationVM = new GenerationViewModel(services);
            TimeShiftVM = new TimeShifterViewModel();
            ReplaceVM = new ReplaceViewModel();
            StrobeEffectVM = new StrobeEffectViewModel();
            LaserLightsVM = new LaserLightsViewModel();

            UndoManager = new UndoManager();

            // Bind collection to UI
            Showlights.Connect()
                .AutoRefresh()
                .Sort(SortExpressionComparer<Showlight>.Ascending(s => s.Time))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out var list)
                .Subscribe();

            ObservableShowlights = list;

            // Subscribe to changes in the collection to set FileDirty
            Showlights.Connect()
                .AutoRefresh()
                .Subscribe(_ =>
                {
                    if (!FileDirty)
                    {
                        FileDirty = true;
                    }
                });

            CreateReactiveCommands();

            UndoManager.FileIsClean
                .Subscribe(_ => FileDirty = false);

            this.WhenAnyValue(x => x.OpenFileName, x => x.FileDirty)
                .Subscribe(UpdateWindowTitle);

            // Update active colors when selected item changes or is edited
            this.WhenAnyValue(x => x.SelectedItem, x => x.SelectedItem.Note, (selected, _) => selected)
                .Where(selected => selected != null)
                .Subscribe(sl =>
                {
                    MoveToTime = sl.Time;
                    InsertTime = sl.Time;

                    UpdateActiveColors(sl);
                });

            this.WhenAnyValue(x => x.ActiveFogColor, x => x.ActiveBeamColor,
                (fog, beam) => $"Fog: {(fog == default ? "N/A" : fog.ToString())}\nBeam: {(beam == default ? "N/A" : beam.ToString())}")
                .ToPropertyEx(this, x => x.PreviewTooltip);
        }

        private void UpdateActiveColors(Showlight selectedShowlight)
        {
            if (selectedShowlight.ShowlightType == ShowlightType.Beam)
            {
                ActiveBeamColor = selectedShowlight.Note;
                ActiveFogColor = FindActiveOrFirstShowlightOfType(ShowlightType.Fog, selectedShowlight.Time)?.Note ?? default;
            }
            else if (selectedShowlight.ShowlightType == ShowlightType.Fog)
            {
                ActiveFogColor = selectedShowlight.Note;
                ActiveBeamColor = FindActiveOrFirstShowlightOfType(ShowlightType.Beam, selectedShowlight.Time)?.Note ?? default;
            }
            else
            {
                ActiveFogColor = FindActiveOrFirstShowlightOfType(ShowlightType.Fog, selectedShowlight.Time)?.Note ?? default;
                ActiveBeamColor = FindActiveOrFirstShowlightOfType(ShowlightType.Beam, selectedShowlight.Time)?.Note ?? default;
            }
        }

        private Showlight FindActiveOrFirstShowlightOfType(ShowlightType type, float time)
            => ObservableShowlights.LastOrDefault(sl => sl.ShowlightType == type && sl.Time <= time) ??
               ObservableShowlights.FirstOrDefault(sl => sl.ShowlightType == type);

        private void CreateReactiveCommands()
        {
            var anyShowlightsPresent = Showlights.CountChanged
                .Select(count => count != 0)
                .DistinctUntilChanged()
                .StartWith(false);

            NewFile = ReactiveCommand.Create(NewFile_Impl);
            OpenFile = ReactiveCommand.Create(OpenFile_Impl);
            SaveFile = ReactiveCommand.Create(SaveFile_Impl, anyShowlightsPresent);
            SaveFileAs = ReactiveCommand.Create(SaveFileAs_Impl, anyShowlightsPresent);

            OptimizeShowlights = ReactiveCommand.Create(OptimizeShowlights_Impl, anyShowlightsPresent);

            DeleteAll = ReactiveCommand.Create<ShowlightType>(DeleteAllOfType, anyShowlightsPresent);

            Insert = ReactiveCommand.Create(Insert_Impl);
            Move = ReactiveCommand.Create(Move_Impl);

            ColorSelect = ReactiveCommand.Create<int>(ColorSelect_Impl);

            Undo = ReactiveCommand.Create(UndoManager.Undo, UndoManager.UndoAvailable);
            Redo = ReactiveCommand.Create(UndoManager.Redo, UndoManager.RedoAvailable);

            var canCopy = this.WhenAnyValue<MainWindowViewModel, bool, Showlight>(x => x.SelectedItem,
                selected => selected != null);

            Cut = ReactiveCommand.Create(Cut_Impl, canCopy);
            Copy = ReactiveCommand.Create(Copy_Impl, canCopy);
            Paste = ReactiveCommand.Create<PasteType>(Paste_Impl);
            //Paste.ThrownExceptions.Subscribe(ex => services.ShowError(ex.Message + Environment.NewLine + ex.StackTrace));

            SetLaserLights = ReactiveCommand.CreateFromTask(SetLaserLights_Impl);
            FindLasersOn = ReactiveCommand.Create(FindLasersOn_Impl, anyShowlightsPresent);

            Generate = ReactiveCommand.CreateFromTask(Generate_Impl);
            ShiftTimes = ReactiveCommand.CreateFromTask(ShiftTimes_Impl, anyShowlightsPresent);
            Replace = ReactiveCommand.CreateFromTask(Replace_Impl, anyShowlightsPresent);
            StrobeEffect = ReactiveCommand.CreateFromTask(StrobeEffect_Impl);

            var canDelete = this.WhenAnyValue<MainWindowViewModel, bool, Showlight>(x => x.SelectedItem, item => item != null);

            Delete = ReactiveCommand.Create(Delete_Impl, canDelete);
        }

        private void UpdateWindowTitle(ValueTuple<string, bool> tuple)
        {
            (string filename, bool filedirty) = tuple;

            string title = string.IsNullOrEmpty(filename) ? ProgramName : $"{filename} - {ProgramName}";
            if (filedirty)
                title = "*" + title;
            WindowTitle = title;
        }

        private void ResetEditor(string filename = null, bool clearShowlights = true)
        {
            if (clearShowlights)
                Showlights.Clear();

            UndoManager.Clear();
            FileDirty = false;

            OpenFileName = filename;
        }

        public bool ConfirmSaveChanges()
        {
            if (FileDirty)
            {
                string message = string.IsNullOrEmpty(OpenFileName) ?
                    "Do you want to save the file?" :
                    $"Save changes to {Path.GetFileName(OpenFileName)}?";

                bool? result = services.QueryUser(message, ProgramName);
                if (result == true)
                {
                    SaveFile_Impl();
                }
                else if (result is null) // The user cancelled out of the dialog
                {
                    return false;
                }
            }

            return true;
        }

        private void NewFile_Impl()
        {
            if (ConfirmSaveChanges())
            {
                ResetEditor();
            }
        }

        private void OpenFile_Impl()
        {
            if(!ConfirmSaveChanges())
            {
                return;
            }

            string filename = services.OpenFileDialog("Open File", "Showlight XML files|*.xml");

            if (filename != null)
            {
                try
                {
                    ShowlightFile showlightFile = XmlHelper.Deserialize<ShowlightFile>(filename);

                    Showlights.Clear();

                    foreach (Showlight sl in showlightFile)
                    {
                        // Old versions of Toolkit have generated undefined notes
                        if (sl.ShowlightType != ShowlightType.Undefined)
                            Showlights.AddOrUpdate(sl);
                    }

                    ResetEditor(filename, clearShowlights: false);
                }
                catch (Exception ex)
                {
                    services.ShowError("Opening the file failed:" + Environment.NewLine + ex.Message);
                }
            }
        }

        private void SaveFileAs_Impl()
        {
            string filename = string.IsNullOrEmpty(OpenFileName) ? "showlights.xml" : Path.GetFileName(OpenFileName);

            if (services.SaveFileDialog(ref filename, "Showlight XML files|*.xml") == true)
            {
                OpenFileName = filename;

                SaveFile_Impl();
            }
        }

        private void SaveFile_Impl()
        {
            if (!string.IsNullOrEmpty(OpenFileName))
            {
                try
                {
                    ShowlightFile.Save(OpenFileName, Showlights.Items.OrderBy(sl => sl.Time));

                    if (FileDirty)
                    {
                        UndoManager.FileWasSaved();
                        FileDirty = false;
                    }
                }
                catch (Exception ex)
                {
                    services.ShowError("Saving the file failed:" + Environment.NewLine + ex.Message);
                }
            }
            else
            {
                SaveFileAs_Impl();
            }
        }

        private void Insert_Impl()
        {
            Showlight newNote = new Showlight(InsertColor, InsertTime);

            UndoManager.AddDelegateUndo(
                "Insert",
                undoAction: () =>
                {
                    Showlights.Remove(newNote);
                    return newNote;
                },
                redoAction: () =>
                {
                    Showlights.AddOrUpdate(newNote);
                    return newNote;
                },
                FileDirty);

            Showlights.AddOrUpdate(newNote);

            SelectedItem = newNote;
            scrollIntoView.OnNext(newNote);
        }

        private void ColorSelect_Impl(int selectedColor)
        {
            if (SelectedItem != null)
            {
                bool anyChanged = false;
                bool fileWasDirty = FileDirty;
                var editedShowlights = SelectedItems.Cast<Showlight>().ToArray();
                UndoEdit undo = new UndoEdit(Showlights, selectedColor);

                foreach (Showlight sl in editedShowlights)
                {
                    if (sl.Note == selectedColor)
                        continue;

                    anyChanged = true;

                    undo.OldShowLights.Add((sl, sl.Note));

                    sl.Note = selectedColor;
                }

                if (!anyChanged)
                    return;

                scrollIntoView.OnNext(editedShowlights[0]);

                UndoManager.AddUndo(undo, fileWasDirty);
            }
        }

        private void Move_Impl()
        {
            if (SelectedItem != null)
            {
                if (SelectedItem.Time == MoveToTime)
                    return;

                UndoManager.AddUndo(new UndoMove(SelectedItem, SelectedItem.Time, MoveToTime), FileDirty);

                SelectedItem.Time = MoveToTime;

                scrollIntoView.OnNext(SelectedItem);
            }
        }

        private void OptimizeShowlights_Impl()
        {
            int activeFog = -1;
            int activeBeam = -1;
            float lasersOnTime = -1.0f;
            float lasersOffTime = -1.0f;

            List<Showlight> removeList = new List<Showlight>();

            foreach (Showlight showlight in ObservableShowlights)
            {
                if (showlight.Note == activeBeam || showlight.Note == activeFog)
                {
                    // Remove duplicate beam or fog notes
                    removeList.Add(showlight);
                }
                else if ((showlight.Note == Showlight.LasersOn && lasersOnTime != -1.0f)
                      || (showlight.Note == Showlight.LasersOff && lasersOffTime != -1.0f))
                {
                    // Remove extra laser on and laser off notes
                    removeList.Add(showlight);
                }

                switch (showlight.ShowlightType)
                {
                    case ShowlightType.Fog:
                        activeFog = showlight.Note;
                        break;

                    case ShowlightType.Beam:
                        activeBeam = showlight.Note;
                        break;

                    case ShowlightType.Laser:
                        if (showlight.Note == Showlight.LasersOn)
                            lasersOnTime = showlight.Time;
                        else
                            lasersOffTime = showlight.Time;
                        break;
                }
            }

            if (removeList.Count > 0)
            {
                // Skip the last one in case it is the workaround fog note
                var last = removeList[removeList.Count - 1];
                if (last.Note == Showlight.FogMax)
                    removeList.Remove(last);

                UndoManager.AddUndo(new UndoRemove("Optimize", Showlights, removeList), FileDirty);

                Showlights.Remove(removeList);
            }
        }

        private void DeleteAllOfType(ShowlightType type)
        {
            var removeList = Showlights.Items.Where(sl => sl.ShowlightType == type).ToList();

            UndoManager.AddUndo(new UndoRemove($"Delete All {type.ToString()}", Showlights, removeList), FileDirty);

            Showlights.Remove(removeList);
        }

        private void Delete_Impl()
        {
            var deletedShowlights = SelectedItems.Cast<Showlight>().ToArray();

            UndoManager.AddUndo(new UndoRemove("Delete", Showlights, deletedShowlights), FileDirty);

            Showlights.Remove(deletedShowlights);
        }

        private void Cut_Impl()
        {
            List<Showlight> selected = SelectedItems
                .Cast<Showlight>()
                .OrderBy(sl => sl.Time)
                .ToList();

            UndoManager.AddUndo(new UndoRemove("Cut", Showlights, selected), FileDirty);

            Showlights.Remove(selected);
            services.SetClipBoardData(selected);
        }

        private void Copy_Impl()
        {
            List<Showlight> selected = SelectedItems
                .Cast<Showlight>()
                .OrderBy(sl => sl.Time)
                .ToList();

            services.SetClipBoardData(selected);
        }

        private List<Showlight> GetOverWritten(List<Showlight> pasted, PasteType pasteType)
        {
            List<Showlight> deleted;

            if (pasteType == PasteType.Replace)
            {
                float startTime = pasted[0].Time;
                float endTime = pasted[pasted.Count - 1].Time;

                deleted = Showlights.Items
                    .Where(sl => sl.Time >= startTime && sl.Time <= endTime)
                    .ToList();
            }
            else
            {
                deleted = new List<Showlight>();
                foreach (var showlight in pasted)
                {
                    var mustReplace = Showlights.Items.FirstOrDefault(sl => sl.Time == showlight.Time && sl.ShowlightType == showlight.ShowlightType);
                    if (mustReplace != null)
                        deleted.Add(mustReplace);
                }
            }

            return deleted;
        }

        private void Paste_Impl(PasteType pasteType)
        {
            List<Showlight> data = services.GetClipBoardData();

            if (data?.Count > 0)
            {
                float oldStartTime = data[0].Time;
                float newStartTime = SelectedItem?.Time ?? oldStartTime;
                float delta = newStartTime - oldStartTime;

                var pastedShowlights =
                    (from sl in data
                     select new Showlight(sl.Note, (float)Math.Round(sl.Time + delta, 3, MidpointRounding.AwayFromZero)))
                    .ToList();

                List<Showlight> deleted = GetOverWritten(pastedShowlights, pasteType);

                Showlight paste()
                {
                    Showlights.Edit(inner =>
                    {
                        inner.Remove(deleted);
                        inner.AddOrUpdate(pastedShowlights);
                    });

                    return pastedShowlights[0];
                }

                string description = (pasteType == PasteType.Replace) ? "Paste Replace" : "Paste Insert";

                UndoManager.AddDelegateUndo(
                    description,
                    undoAction: () =>
                    {
                        Showlights.Edit(inner =>
                        {
                            inner.Remove(pastedShowlights);
                            inner.AddOrUpdate(deleted);
                        });
                        return deleted.Count > 0 ? deleted[0] : null;
                    },
                    redoAction: paste,
                    FileDirty);

                paste();

                SelectedItem = pastedShowlights[0];
            }
        }

        private async Task Generate_Impl()
        {
            GenerationVM.CurrentShowlights = Showlights.Items;

            using (DisableEditor())
            {
                await GenerationVM.ShowDialog();

                if (GenerationVM.ShowlightsList != null)
                {
                    var oldShowlights = Showlights.Items.ToList();
                    var newShowLights = GenerationVM.ShowlightsList;

                    UndoManager.AddDelegateUndo(
                        "Generate",
                        undoAction: () =>
                        {
                            Showlights.Edit(inner => inner.Load(oldShowlights));
                            return null;
                        },
                        redoAction: () =>
                        {
                            Showlights.Edit(inner => inner.Load(newShowLights));
                            return null;
                        },
                        FileDirty);

                    Showlights.Edit(inner => inner.Load(newShowLights));
                }
            }
        }

        private IDisposable DisableEditor()
        {
            EditorEnabled = false;
            return Disposable.Create(() => EditorEnabled = true);
        }

        private void FindLasersOn_Impl()
        {
            Showlight lasersOnNote = Showlights.Items.FirstOrDefault(sl => sl.Note == Showlight.LasersOn);
            if (lasersOnNote is null)
            {
                services.ShowError("Not found.");
            }
            else
            {
                SelectedItem = lasersOnNote;
                scrollIntoView.OnNext(lasersOnNote);
            }
        }

        private async Task SetLaserLights_Impl()
        {
            using (DisableEditor())
            {
                Showlight oldOn = Showlights.Items.FirstOrDefault(sl => sl.Note == Showlight.LasersOn);
                Showlight oldOff = Showlights.Items.FirstOrDefault(sl => sl.Note == Showlight.LasersOff);
                if (oldOn != null)
                {
                    LaserLightsVM.OnTime = oldOn.Time;
                }
                if (oldOff != null)
                {
                    LaserLightsVM.OffTime = oldOff.Time;
                }

                bool resultOk = await LaserLightsVM.ShowDialog();

                if (resultOk)
                {
                    var removed = new List<Showlight>();
                    if (oldOn != null)
                        removed.Add(oldOn);
                    if (oldOff != null)
                        removed.Add(oldOff);

                    var added = new List<Showlight>
                    {
                        new Showlight(Showlight.LasersOn, LaserLightsVM.OnTime),
                        new Showlight(Showlight.LasersOff, LaserLightsVM.OffTime)
                    };

                    Showlight setLasers()
                    {
                        Showlights.Edit(inner =>
                        {
                            if (removed.Count > 0)
                                inner.Remove(removed);
                            inner.AddOrUpdate(added);
                        });

                        return null;
                    }

                    UndoManager.AddDelegateUndo(
                        "Set Laserlights",
                        undoAction: () =>
                        {
                            Showlights.Edit(inner =>
                            {
                                inner.Remove(added);
                                if (removed.Count > 0)
                                    inner.AddOrUpdate(removed);
                            });

                            return null;
                        },
                        redoAction: setLasers,
                        FileDirty);

                    setLasers();
                }
            }
        }

        private async Task ShiftTimes_Impl()
        {
            using (DisableEditor())
            {
                bool resultOk = await TimeShiftVM.ShowDialog();

                if (resultOk && Showlights.Count > 0 && TimeShiftVM.ShiftAmount != 0f)
                {
                    var shiftedShowlights = new List<Showlight>(Showlights.Count);

                    foreach (var sl in Showlights.Items)
                    {
                        float newTime = (float)Math.Round(sl.Time + TimeShiftVM.ShiftAmount, 3, MidpointRounding.AwayFromZero);

                        // Skip any notes with negative times
                        if (newTime < 0f)
                            continue;

                        shiftedShowlights.Add(new Showlight(sl.Note, newTime));
                    }

                    var oldShowlights = Showlights.Items.ToList();

                    UndoManager.AddDelegateUndo(
                        "Shift Times",
                        undoAction: () =>
                        {
                            Showlights.Edit(inner => inner.Load(oldShowlights));
                            return null;
                        },
                        redoAction: () =>
                        {
                            Showlights.Edit(inner => inner.Load(shiftedShowlights));
                            return null;
                        },
                        FileDirty);

                    Showlights.Edit(inner => inner.Load(shiftedShowlights));
                }
            }
        }

        private async Task Replace_Impl()
        {
            using (DisableEditor())
            {
                if (SelectedItem != null)
                {
                    ReplaceVM.OriginalColor = SelectedItem.Note;
                }

                ReplaceVM.OriginalShowlights = Showlights.Items;

                if (SelectedItems != null)
                {
                    ReplaceVM.SelectedShowlights = SelectedItems.Cast<Showlight>().ToList();
                    ReplaceVM.SelectionOnlyEnabled = true;
                }
                else
                {
                    ReplaceVM.SelectedShowlights = null;
                    ReplaceVM.SelectionOnlyEnabled = false;
                }

                bool resultOk = await ReplaceVM.ShowDialog();

                if (resultOk)
                {
                    int oldColor = ReplaceVM.OriginalColor;
                    int newColor = ReplaceVM.ReplaceWithColor;

                    IEnumerable<Showlight> showlights = ReplaceVM.SelectionOnly ? ReplaceVM.SelectedShowlights : Showlights.Items;
                    var replacedShowlights = showlights.Where(sl => sl.Note == oldColor).ToList();

                    if (replacedShowlights.Count > 0)
                    {
                        UndoManager.AddDelegateUndo(
                            "Replace",
                            undoAction: () =>
                            {
                                replacedShowlights.ForEach(sl => sl.Note = oldColor);

                                return null;
                            },
                            redoAction: () =>
                            {
                                replacedShowlights.ForEach(sl => sl.Note = newColor);

                                return null;
                            },
                            FileDirty);

                        replacedShowlights.ForEach(sl => sl.Note = newColor);
                    }
                }
            }
        }

        private async Task StrobeEffect_Impl()
        {
            using (DisableEditor())
            {
                if (SelectedItem != null)
                {
                    StrobeEffectVM.StartTime = SelectedItem.Time;
                    StrobeEffectVM.EndTime = (float)Math.Round(StrobeEffectVM.StartTime + 1f, 3, MidpointRounding.AwayFromZero);
                }
                else if(StrobeEffectVM.EndTime == 0f)
                {
                    StrobeEffectVM.EndTime = (float)Math.Round(StrobeEffectVM.StartTime + 1f, 3, MidpointRounding.AwayFromZero);
                }

                bool resultOk = await StrobeEffectVM.ShowDialog();

                if (resultOk)
                {
                    var generated = StrobeEffectVM.GeneratedShowlights;
                    if (generated.Count == 0)
                        return;

                    ShowlightType type = Showlight.GetShowlightType(StrobeEffectVM.Color1);
                    float lastGeneratedTime = generated[generated.Count - 1].Time;
                    var deleted = Showlights.Items
                        .Where(sl => sl.Time >= StrobeEffectVM.StartTime && sl.Time <= lastGeneratedTime && sl.ShowlightType == type)
                        .ToList();

                    Showlight doStrobeEffect()
                    {
                        Showlights.Edit(inner =>
                        {
                            inner.Remove(deleted);
                            inner.AddOrUpdate(generated);
                        });
                        return null;
                    }

                    UndoManager.AddDelegateUndo(
                        "Strobe Effect",
                        undoAction: () =>
                        {
                            Showlights.Edit(inner =>
                            {
                                inner.Remove(generated);
                                inner.AddOrUpdate(deleted);
                            });
                            return null;
                        },
                        redoAction: doStrobeEffect,
                        FileDirty);

                    doStrobeEffect();
                }
            }
        }

        public void SavePreferences()
        {
            GenerationVM.SavePreferences();
        }
    }
}
