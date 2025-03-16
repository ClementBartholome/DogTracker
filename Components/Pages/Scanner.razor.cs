using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.IO;
using DogTracker.Components.Dialog;
using DogTracker.Enums;
using DogTracker.Models;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace DogTracker.Components.Pages
{
    public partial class Scanner : ComponentBase, IAsyncDisposable
    {
        private bool isCaptureMode = false;
        private string? previewImage;
        private List<GedFile> scannedFiles = new();
        private bool _open;
        private Anchor _anchor;
        private bool _overlayAutoClose = true;
        private bool _showCategoryDialog;
        private TypeFilesEnum _selectedCategory;
        private TypeFilesEnum _filterCategory ;
        private bool hasFlash = false;
        private bool flashEnabled = false;
        
        IList<IBrowserFile> _files = new List<IBrowserFile>();
        [Inject] private IDialogService DialogService { get; set; } = null!;
        [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
        [Inject] private IWebHostEnvironment Environment { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Parameter] public int DogId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadExistingFiles();
        }

        private async Task LoadExistingFiles()
        {
            var uploadsPath = Path.Combine(Environment.WebRootPath, "uploads", DogId.ToString());
            if (Directory.Exists(uploadsPath))
            {
                var files = Directory.GetFiles(uploadsPath, "scan-*.jpg")
                    .Select(f => new GedFile
                    {
                        PreviewUrl = $"/uploads/{DogId}/{Path.GetFileName(f)}",
                        DownloadUrl = $"/uploads/{DogId}/{Path.GetFileName(f)}",
                        Category = Enum.Parse<TypeFilesEnum>(GetCategoryFromFileName(Path.GetFileName(f))),
                        CreatedAt = File.GetCreationTime(f)
                    })
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();
                scannedFiles = files;
            }
        }

        private string GetCategoryFromFileName(string fileName)
        {
            var parts = fileName.Split('-');
            return parts.Length >= 3 ? parts[1] : "misc";
        }

        private async Task StartScanning()
        {
            CloseDrawer();
            isCaptureMode = true;
            await JsRuntime.InvokeVoidAsync("initializeCamera", DotNetObjectReference.Create(this));
        }

        private async Task CapturePhoto()
        {
            previewImage = await JsRuntime.InvokeAsync<string>("takePhoto");
        }

        [JSInvokable]
        public void OnPhotoTaken(string data)
        {
            previewImage = data;
            StateHasChanged();
        }

        private async Task OpenCategoryDialog()
        {
            var parameters = new DialogParameters
            {
                { "SaveScanCallback", EventCallback.Factory.Create(this, SaveScan) }
            };
            var dialog = DialogService.Show<FileCategoryDialog>("Sélectionner la catégorie du document", parameters);
            var result = await dialog.Result;
            if (result is { Canceled: false, Data: TypeFilesEnum typeFile })
            {
                _selectedCategory = typeFile;
                await SaveScan();
            }
        }
        
        private void OpenPreviewDialog(GedFile file)
        {
            var parameters = new DialogParameters
            {
                { "File", file }
            };
    
            var options = new DialogOptions 
            { 
                CloseButton = true,
                FullScreen = false,
                MaxWidth = MaxWidth.Large
            };
    
            DialogService.ShowAsync<FilePreviewDialog>("Aperçu du document", parameters, options);
        }

        private async Task SaveScan()
        {
            if (string.IsNullOrEmpty(previewImage) || _selectedCategory == null)
                return;

            var base64Data = previewImage.Split(',')[1];
            var imageBytes = Convert.FromBase64String(base64Data);

            var uploadsPath = Path.Combine(Environment.WebRootPath, "uploads", DogId.ToString());
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"scan-{_selectedCategory}-{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var filePath = Path.Combine(uploadsPath, fileName);

            await File.WriteAllBytesAsync(filePath, imageBytes);

            var newFile = new GedFile
            {
                PreviewUrl = $"/uploads/{DogId}/{fileName}",
                DownloadUrl = $"/uploads/{DogId}/{fileName}",
                Category = _selectedCategory,
                CreatedAt = DateTime.Now
            };

            scannedFiles.Insert(0, newFile);
            
            Snackbar.Add("Document enregistré avec succès", Severity.Success);

            previewImage = null;
            isCaptureMode = false;
            _showCategoryDialog = false;
            await StopCamera();
        }

        private void UploadFiles(IBrowserFile file)
        {
            _files.Add(file);
            _selectedCategory = TypeFilesEnum.Divers;
            _showCategoryDialog = true;
        }

        private async Task RetakePhoto()
        {
            previewImage = null;
            isCaptureMode = true;
            await JsRuntime.InvokeVoidAsync("initializeCamera", DotNetObjectReference.Create(this));
        }

        private async Task StopScanning()
        {
            isCaptureMode = false;
            previewImage = null;
            await StopCamera();
        }

        private async Task StopCamera()
        {
            await JsRuntime.InvokeVoidAsync("stopCamera");
        }

        public async ValueTask DisposeAsync()
        {
            await StopCamera();
        }

        private async Task DeleteFile(GedFile file)
        {
            var parameters = new DialogParameters
            {
                { "ContentText", "Êtes-vous sûr de vouloir supprimer ce document ?" },
                { "ButtonText", "Supprimer" },
                { "Color", Color.Error }
            };

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
            var dialog = DialogService.Show<Dialog.Dialog>("Confirmation", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                try
                {
                    var fileName = Path.GetFileName(file.PreviewUrl);
                    var filePath = Path.Combine(Environment.WebRootPath, "uploads", DogId.ToString(), fileName);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        scannedFiles.Remove(file);
                        Snackbar.Add("Document supprimé", Severity.Success);
                        StateHasChanged();
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add("Erreur lors de la suppression", Severity.Error);
                    Console.Error.WriteLine($"Erreur lors de la suppression du fichier : {ex.Message}");
                }
            }
        }
        
        private void OpenDrawer(Anchor anchor)
        {
            _open = true;
            _anchor = anchor;
        }

        private void CloseDrawer()
        {
            _open = false;
        }
        
        private Color GetChipColor(TypeFilesEnum category)
        {
            return category switch
            {
                TypeFilesEnum.Ordonnances => Color.Info,
                TypeFilesEnum.CarnetDeSante => Color.Success,
                TypeFilesEnum.Divers => Color.Warning,
                _ => Color.Default
            };
        }

        [JSInvokable]
        public void SetFlashAvailability(bool available)
        {
            hasFlash = available;
            StateHasChanged();
        }

        private async Task ToggleFlash()
        {
            flashEnabled = !flashEnabled;
            await JsRuntime.InvokeVoidAsync("toggleFlash", flashEnabled);
        }
    }
}