using DynamicData;
using DynamicData.Binding;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Rocksmith2014.XML;

using ShowlightEditor.Core.Models;
using ShowlightEditor.Core.Services;

using ShowLightGenerator;

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

namespace ShowlightEditor.Core.ViewModels
{
    public sealed class MainWindowViewModel : ReactiveObject
    {
        public const string ProgramName = "Showlight Editor";

        public ReadOnlyObservableCollection<ShowLightViewModel> ObservableShowlights { get; }
        public UndoManager<ShowLightViewModel> UndoManager { get; }

        private SourceCache<ShowLightViewModel, int> Showlights { get; } = new SourceCache<ShowLightViewModel, int>(sl => sl.Id);

        private readonly IPlatformSpecificServices services;

        public GenerationViewModel GenerationVM { get; }
        public TimeShifterViewModel TimeShiftVM { get; }
        public ReplaceViewModel ReplaceVM { get; }
        public StrobeEffectViewModel StrobeEffectVM { get; }
        public LaserLightsViewModel LaserLightsVM { get; }

        private readonly Subject<ShowLightViewModel> scrollIntoView = new Subject<ShowLightViewModel>();
        public IObservable<ShowLightViewModel> ScrollIntoView => scrollIntoView;

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
        public ShowLightViewModel SelectedItem { get; set; }

        public IList SelectedItems { get; set; }

        public byte InsertColor { get; set; }

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
        public ReactiveCommand<PasteType, Unit> Paste { get; private set; }
        public ReactiveCommand<Unit, Unit> Undo { get; private set; }
        public ReactiveCommand<Unit, Unit> Redo { get; private set; }
        public ReactiveCommand<Unit, Unit> Delete { get; private set; }

        public ReactiveCommand<ShowLightType, Unit> DeleteAll { get; private set; }
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
            var sl1 = new ShowLightViewModel(ShowLight.FogMax, 10_000);
            var sl2 = new ShowLightViewModel(ShowLight.BeamMin, 11_000);
            var sl3 = new ShowLightViewModel(ShowLight.LasersOn, 12_000);
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

            UndoManager = new UndoManager<ShowLightViewModel>();

            // Bind collection to UI
            Showlights.Connect()
                .AutoRefresh()
                .Sort(SortExpressionComparer<ShowLightViewModel>.Ascending(s => s.Time))
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
                .Where(selected => selected is not null)
                .Subscribe(sl =>
                {
                    MoveToTime = sl.Time / 1000f;
                    InsertTime = sl.Time / 1000f;

                    UpdateActiveColors(sl);
                });

            this.WhenAnyValue(x => x.ActiveFogColor, x => x.ActiveBeamColor,
                (fog, beam) => $"Fog: {(fog == default ? "N/A" : fog.ToString())}\nBeam: {(beam == default ? "N/A" : beam.ToString())}")
                .ToPropertyEx(this, x => x.PreviewTooltip);
        }

        private void UpdateActiveColors(ShowLightViewModel selectedShowlight)
        {
            if (selectedShowlight.ShowlightType == ShowLightType.Beam)
            {
                ActiveBeamColor = selectedShowlight.Note;
                ActiveFogColor = FindActiveOrFirstShowlightOfType(ShowLightType.Fog, selectedShowlight.Time)?.Note ?? default;
            }
            else if (selectedShowlight.ShowlightType == ShowLightType.Fog)
            {
                ActiveFogColor = selectedShowlight.Note;
                ActiveBeamColor = FindActiveOrFirstShowlightOfType(ShowLightType.Beam, selectedShowlight.Time)?.Note ?? default;
            }
            else
            {
                ActiveFogColor = FindActiveOrFirstShowlightOfType(ShowLightType.Fog, selectedShowlight.Time)?.Note ?? default;
                ActiveBeamColor = FindActiveOrFirstShowlightOfType(ShowLightType.Beam, selectedShowlight.Time)?.Note ?? default;
            }
        }

        private ShowLightViewModel FindActiveOrFirstShowlightOfType(ShowLightType type, float time)
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

            DeleteAll = ReactiveCommand.Create<ShowLightType>(DeleteAllOfType, anyShowlightsPresent);

            Insert = ReactiveCommand.Create(Insert_Impl);
            Move = ReactiveCommand.Create(Move_Impl);

            ColorSelect = ReactiveCommand.Create<int>(ColorSelect_Impl);

            Undo = ReactiveCommand.Create(UndoManager.Undo, UndoManager.UndoAvailable);
            Redo = ReactiveCommand.Create(UndoManager.Redo, UndoManager.RedoAvailable);

            var canCopy = this.WhenAnyValue<MainWindowViewModel, bool, ShowLightViewModel>(x => x.SelectedItem,
                selected => selected is not null);

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

            var canDelete = this.WhenAnyValue<MainWindowViewModel, bool, ShowLightViewModel>(x => x.SelectedItem, item => item is not null);

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

        public ConfirmSaveResult ConfirmSaveChanges()
        {
            if (FileDirty)
            {
                string message = string.IsNullOrEmpty(OpenFileName) ?
                    "Do you want to save the file?" :
                    $"Save changes to {Path.GetFileName(OpenFileName)}?";

                UserChoice result = services.QueryUser(message, ProgramName);
                if (result == UserChoice.Yes)
                {
                    SaveFile_Impl();
                }
                else if (result == UserChoice.Cancel)
                {
                    return ConfirmSaveResult.Cancel;
                }
            }

            return ConfirmSaveResult.Ok;
        }

        private void NewFile_Impl()
        {
            if (ConfirmSaveChanges() == ConfirmSaveResult.Ok)
            {
                ResetEditor();
            }
        }

        private void OpenFile_Impl()
        {
            if (ConfirmSaveChanges() == ConfirmSaveResult.Cancel)
            {
                return;
            }

            string filename = services.OpenFileDialog("Open File", "Showlight XML files|*.xml");

            if (filename is not null)
            {
                try
                {
                    var showlightFile = ShowLights.Load(filename);

                    Showlights.Clear();

                    foreach (var sl in showlightFile)
                    {
                        // Old versions of Toolkit have generated undefined notes
                        if (sl.GetShowLightType() != ShowLightType.Undefined)
                            Showlights.AddOrUpdate(new ShowLightViewModel(sl));
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
                    var list = Showlights.Items
                        .OrderBy(sl => sl.Time)
                        .Select(x => x.Model)
                        .ToList();
                    ShowLights.Save(OpenFileName, list);

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
            var newNote = new ShowLightViewModel(InsertColor, (int)(InsertTime * 1000f));

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
            if (SelectedItem is not null)
            {
                bool anyChanged = false;
                bool fileWasDirty = FileDirty;
                var editedShowlights = SelectedItems.Cast<ShowLightViewModel>().ToArray();
                var oldShowlights = new List<(ShowLightViewModel, int)>();

                foreach (var sl in editedShowlights)
                {
                    if (sl.Note == selectedColor)
                        continue;

                    anyChanged = true;

                    oldShowlights.Add((sl, sl.Note));

                    sl.Note = (byte)selectedColor;
                }

                if (!anyChanged)
                    return;

                var undo = new UndoEdit(oldShowlights, selectedColor);

                scrollIntoView.OnNext(editedShowlights[0]);

                UndoManager.AddUndo(undo, fileWasDirty);
            }
        }

        private void Move_Impl()
        {
            if (SelectedItem is not null)
            {
                int timeSec = (int)(MoveToTime * 1000f);
                if (SelectedItem.Time == timeSec)
                    return;

                UndoManager.AddUndo(new UndoMove(SelectedItem, SelectedItem.Time, timeSec), FileDirty);

                SelectedItem.Time = timeSec;

                scrollIntoView.OnNext(SelectedItem);
            }
        }

        private void OptimizeShowlights_Impl()
        {
            int activeFog = -1;
            int activeBeam = -1;
            int lasersOnTime = -1;
            int lasersOffTime = -1;

            List<ShowLightViewModel> removeList = new List<ShowLightViewModel>();

            foreach (var showlight in ObservableShowlights)
            {
                if (showlight.Note == activeBeam || showlight.Note == activeFog)
                {
                    // Remove duplicate beam or fog notes
                    removeList.Add(showlight);
                }
                else if ((showlight.Note == ShowLight.LasersOn && lasersOnTime != -1)
                      || (showlight.Note == ShowLight.LasersOff && lasersOffTime != -1))
                {
                    // Remove extra laser on and laser off notes
                    removeList.Add(showlight);
                }

                switch (showlight.ShowlightType)
                {
                    case ShowLightType.Fog:
                        activeFog = showlight.Note;
                        break;

                    case ShowLightType.Beam:
                        activeBeam = showlight.Note;
                        break;

                    case ShowLightType.Laser:
                        if (showlight.Note == ShowLight.LasersOn)
                            lasersOnTime = showlight.Time;
                        else
                            lasersOffTime = showlight.Time;
                        break;
                }
            }

            if (removeList.Count > 0)
            {
                // Skip the last one in case it is the workaround fog note
                var last = removeList[^1];
                if (last.Note == ShowLight.FogMax)
                    removeList.Remove(last);

                UndoManager.AddUndo(new UndoRemove("Optimize", Showlights, removeList), FileDirty);

                Showlights.Remove(removeList);
            }
        }

        private void DeleteAllOfType(ShowLightType type)
        {
            var removeList = Showlights.Items.Where(sl => sl.ShowlightType == type).ToList();

            UndoManager.AddUndo(new UndoRemove($"Delete All {type}", Showlights, removeList), FileDirty);

            Showlights.Remove(removeList);
        }

        private void Delete_Impl()
        {
            var deletedShowlights = SelectedItems.Cast<ShowLightViewModel>().ToArray();

            UndoManager.AddUndo(new UndoRemove("Delete", Showlights, deletedShowlights), FileDirty);

            Showlights.Remove(deletedShowlights);
        }

        private void Cut_Impl()
        {
            List<ShowLightViewModel> selected = SelectedItems
                .Cast<ShowLightViewModel>()
                .OrderBy(sl => sl.Time)
                .ToList();

            UndoManager.AddUndo(new UndoRemove("Cut", Showlights, selected), FileDirty);

            Showlights.Remove(selected);
            services.SetClipBoardData(selected);
        }

        private void Copy_Impl()
        {
            List<ShowLightViewModel> selected = SelectedItems
                .Cast<ShowLightViewModel>()
                .OrderBy(sl => sl.Time)
                .ToList();

            services.SetClipBoardData(selected);
        }

        private List<ShowLightViewModel> GetOverWritten(List<ShowLightViewModel> pasted, PasteType pasteType)
        {
            List<ShowLightViewModel> deleted;

            if (pasteType == PasteType.Replace)
            {
                int startTime = pasted[0].Time;
                int endTime = pasted[^1].Time;

                deleted = Showlights.Items
                    .Where(sl => sl.Time >= startTime && sl.Time <= endTime)
                    .ToList();
            }
            else
            {
                deleted = new List<ShowLightViewModel>();
                foreach (var showlight in pasted)
                {
                    var mustReplace = Showlights.Items.FirstOrDefault(sl => sl.Time == showlight.Time && sl.ShowlightType == showlight.ShowlightType);
                    if (mustReplace is not null)
                        deleted.Add(mustReplace);
                }
            }

            return deleted;
        }

        private void Paste_Impl(PasteType pasteType)
        {
            List<ShowLightViewModel> data = services.GetClipBoardData();

            if (data?.Count > 0)
            {
                int oldStartTime = data[0].Time;
                int newStartTime = SelectedItem?.Time ?? oldStartTime;
                int delta = newStartTime - oldStartTime;

                var pastedShowlights =
                    (from sl in data
                     select new ShowLightViewModel(sl.Note, sl.Time + delta))
                    .ToList();

                var deleted = GetOverWritten(pastedShowlights, pasteType);

                ShowLightViewModel paste()
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

                if (GenerationVM.ShowlightsList is not null)
                {
                    var oldShowlights = Showlights.Items.ToList();
                    var newShowLights = GenerationVM.ShowlightsList.Select(x => new ShowLightViewModel(x));

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
            var lasersOnNote = Showlights.Items.FirstOrDefault(sl => sl.Note == ShowLight.LasersOn);
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
                var oldOn = Showlights.Items.FirstOrDefault(sl => sl.Note == ShowLight.LasersOn);
                var oldOff = Showlights.Items.FirstOrDefault(sl => sl.Note == ShowLight.LasersOff);
                if (oldOn is not null)
                {
                    LaserLightsVM.OnTime = oldOn.Time;
                }
                if (oldOff is not null)
                {
                    LaserLightsVM.OffTime = oldOff.Time;
                }

                bool resultOk = await LaserLightsVM.ShowDialog();

                if (resultOk)
                {
                    var removed = new List<ShowLightViewModel>();
                    if (oldOn is not null)
                        removed.Add(oldOn);
                    if (oldOff is not null)
                        removed.Add(oldOff);

                    var added = new List<ShowLightViewModel>
                    {
                        new ShowLightViewModel(ShowLight.LasersOn, LaserLightsVM.OnTime),
                        new ShowLightViewModel(ShowLight.LasersOff, LaserLightsVM.OffTime)
                    };

                    ShowLightViewModel setLasers()
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

                    _ = setLasers();
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
                    var shiftedShowlights = new List<ShowLightViewModel>(Showlights.Count);

                    foreach (var sl in Showlights.Items)
                    {
                        int newTime = (int)Math.Round(sl.Time + TimeShiftVM.ShiftAmount * 1000f, 3, MidpointRounding.AwayFromZero);

                        // Skip any notes with negative times
                        if (newTime < 0f)
                            continue;

                        shiftedShowlights.Add(new ShowLightViewModel(sl.Note, newTime));
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
                if (SelectedItem is not null)
                {
                    ReplaceVM.OriginalColor = SelectedItem.Note;
                }

                ReplaceVM.OriginalShowlights = Showlights.Items;

                if (SelectedItems is not null)
                {
                    ReplaceVM.SelectedShowlights = SelectedItems.Cast<ShowLightViewModel>().ToList();
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
                    byte oldColor = (byte)ReplaceVM.OriginalColor;
                    byte newColor = (byte)ReplaceVM.ReplaceWithColor;

                    var showlights = ReplaceVM.SelectionOnly ? ReplaceVM.SelectedShowlights : Showlights.Items;
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
                if (SelectedItem is not null)
                {
                    StrobeEffectVM.StartTime = SelectedItem.Time;
                    StrobeEffectVM.EndTime = (float)Math.Round(StrobeEffectVM.StartTime + 1f, 3, MidpointRounding.AwayFromZero);
                }
                else if (StrobeEffectVM.EndTime == 0f)
                {
                    StrobeEffectVM.EndTime = (float)Math.Round(StrobeEffectVM.StartTime + 1f, 3, MidpointRounding.AwayFromZero);
                }

                bool resultOk = await StrobeEffectVM.ShowDialog();

                if (resultOk)
                {
                    var generated = StrobeEffectVM.GeneratedShowlights;
                    if (generated.Count == 0)
                        return;

                    var type = ShowLightViewModel.GetShowlightType(StrobeEffectVM.Color1);
                    int lastGeneratedTime = generated[^1].Time;
                    var deleted = Showlights.Items
                        .Where(sl => sl.Time >= StrobeEffectVM.StartTime && sl.Time <= lastGeneratedTime && sl.ShowlightType == type)
                        .ToList();

                    ShowLightViewModel doStrobeEffect()
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
