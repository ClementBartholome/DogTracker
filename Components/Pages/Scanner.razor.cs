using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.IO;
using DogTracker.Components.Dialog;
using DogTracker.Enums;
using DogTracker.Models;
using DogTracker.Services;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace DogTracker.Components.Pages
{
    public partial class Scanner : ComponentBase, IAsyncDisposable
    {
        private bool isCaptureMode;
        private string? previewImage;
        private List<GedFile> scannedFiles = [];
        private bool _open;
        private Anchor _anchor;
        private readonly bool _overlayAutoClose = true;
        private bool _showCategoryDialog;
        private TypeFilesEnum _selectedCategory;
        private string _selectedDocumentName;
        private TypeFilesEnum _filterCategory ;
        private bool hasFlash;
        private bool flashEnabled;


        private readonly List<IBrowserFile> _files = [];
        [Inject] private IDialogService DialogService { get; set; } = null!;
        [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
        [Inject] private IWebHostEnvironment Environment { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Parameter] public int DogId { get; set; }
        [Inject] private BlobStorageService BlobStorageService { get; set; } = null!;


        protected override async Task OnInitializedAsync()
        {
            await LoadExistingFiles();
        }

        private async Task LoadExistingFiles()
        {
            var containerName = $"dog-{DogId}";
            var containerClient = BlobStorageService.GetContainerClient(containerName);

            if (await containerClient.ExistsAsync())
            {
                scannedFiles = containerClient.GetBlobs()
                    .Select(blob => new GedFile
                    {
                        Name = GetNameFromFileName(blob.Name),
                        PreviewUrl = containerClient.GetBlobClient(blob.Name).Uri.ToString(),
                        DownloadUrl = containerClient.GetBlobClient(blob.Name).Uri.ToString(),
                        CreatedAt = blob.Properties.CreatedOn?.DateTime ?? DateTime.Now,
                        Category = Enum.Parse<TypeFilesEnum>(GetCategoryFromFileName(blob.Name))
                    })
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();
            }
        }

        private string GetIconForFileType(string extension)
        {
            return extension switch
            {
                ".pdf" => "/images/icons/pdf-preview.png",
                ".doc" or ".docx" => "/images/icons/doc-preview.png",
                ".xls" or ".xlsx" => "/images/icons/xls-preview.png",
                _ => "/images/icons/file-preview.png"
            };
        }
        private string GetCategoryFromFileName(string fileName)
        {
            var parts = fileName.Split('-');
            return parts.Length >= 6 ? parts[7] : "Divers";
        }
        
        private string GetNameFromFileName(string fileName)
        {
            var parts = fileName.Split('-');
            // Format : [GUID]-scan-[Nom]-[Catégorie]-[Date].jpg
            return parts.Length >= 5 ? parts[6] : fileName;
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
    
            if (result is { Canceled: false, Data: FileDialogResult fileResult })
            {
                _selectedCategory = fileResult.Category;
                _selectedDocumentName = fileResult.Name;
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
            if (string.IsNullOrEmpty(previewImage) || string.IsNullOrEmpty(_selectedDocumentName))
                return;

            var base64Data = previewImage.Split(',')[1];
            var imageBytes = Convert.FromBase64String(base64Data);

            var containerName = $"dog-{DogId}";
            var fileName = $"scan-{_selectedDocumentName}-{_selectedCategory}-{DateTime.Now:yyyyMMddHHmmss}.jpg";

            using var stream = new MemoryStream(imageBytes);
            var blobUrl = await BlobStorageService.UploadFileAsync(new BrowserFileWrapper(stream, fileName), containerName);

            scannedFiles.Insert(0, new GedFile
            {
                Name = _selectedDocumentName,
                PreviewUrl = blobUrl,
                DownloadUrl = blobUrl,
                Category = _selectedCategory,
                CreatedAt = DateTime.Now
            });

            Snackbar.Add("Document enregistré avec succès", Severity.Success);

            previewImage = null;
            isCaptureMode = false;
            _selectedDocumentName = null;
        }

        private async Task UploadFiles(IBrowserFile file)
        {
            try
            {
                _files.Clear(); // Clear previous files
                _files.Add(file);
        
                // Generate a preview for the selected file
                await GenerateFilePreview(file);
        
                // Show the category dialog
                await OpenCategoryDialogForUpload();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Erreur lors du téléchargement: {ex.Message}", Severity.Error);
            }
        }
        
        private async Task GenerateFilePreview(IBrowserFile file)
        {
            // For images, create a preview
            if (file.ContentType.StartsWith("image/"))
            {
                var imageFile = await file.RequestImageFileAsync(file.ContentType, 800, 600);
                using var stream = imageFile.OpenReadStream(maxAllowedSize: 10485760); // 10MB max
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                previewImage = $"data:{file.ContentType};base64,{Convert.ToBase64String(imageBytes)}";
            }
            else
            {
                // For non-image files, use a generic preview based on file extension
                var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                string iconPath = extension switch
                {
                    ".pdf" => "/images/icons/pdf-icon.png",
                    ".doc" or ".docx" => "/images/icons/doc-icon.png",
                    ".xls" or ".xlsx" => "/images/icons/xls-icon.png",
                    _ => "/images/icons/file-icon.png"
                };
        
                previewImage = iconPath;
            }
    
            isCaptureMode = true;
        }
        
        private async Task OpenCategoryDialogForUpload()
        {
            var parameters = new DialogParameters
            {
                { "SaveScanCallback", EventCallback.Factory.Create(this, SaveUploadedFile) }
            };

            var dialog = DialogService.Show<FileCategoryDialog>("Sélectionner la catégorie du document", parameters);
            var result = await dialog.Result;

            if (result is { Canceled: false, Data: FileDialogResult fileResult })
            {
                _selectedCategory = fileResult.Category;
                _selectedDocumentName = fileResult.Name;
                await SaveUploadedFile();
            }
            else
            {
                // User canceled, reset state
                previewImage = null;
                isCaptureMode = false;
                _files.Clear();
            }
        }
        
        private async Task SaveUploadedFile()
        {
            if (_files.Count == 0 || string.IsNullOrEmpty(_selectedDocumentName))
                return;
    
            var file = _files[0];
            var extension = Path.GetExtension(file.Name);
            if (string.IsNullOrEmpty(extension))
                extension = ".bin";
    
            var uploadsPath = Path.Combine(Environment.WebRootPath, "uploads", DogId.ToString());
            Directory.CreateDirectory(uploadsPath);
    
            var fileName = $"upload-{_selectedDocumentName}-{_selectedCategory}-{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            await using (var stream = file.OpenReadStream(maxAllowedSize: 10485760)) // 10MB
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }
    
            // Create thumbnail for non-image files
            string previewUrl = $"/uploads/{DogId}/{fileName}";
            if (!file.ContentType.StartsWith("image/"))
            {
                // For non-image files, use a generic preview based on file type
                previewUrl = file.ContentType switch
                {
                    "application/pdf" => "/images/icons/pdf-preview.png",
                    "application/msword" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" 
                        => "/images/icons/doc-preview.png",
                    _ => "/images/icons/file-preview.png"
                };
            }
    
            var newFile = new GedFile
            {
                Name = _selectedDocumentName,
                PreviewUrl = previewUrl,
                DownloadUrl = $"/uploads/{DogId}/{fileName}",
                Category = _selectedCategory,
                CreatedAt = DateTime.Now
            };
    
            scannedFiles.Insert(0, newFile);
    
            Snackbar.Add("Document téléchargé avec succès", Severity.Success);
    
            previewImage = null;
            _selectedDocumentName = null;
            isCaptureMode = false;
            _files.Clear();
            CloseDrawer();
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

            if (result is not { Canceled: true })
            {
                try
                {
                    await BlobStorageService.DeleteFileAsync(file.DownloadUrl);
                    scannedFiles.Remove(file);
                    Snackbar.Add("Document supprimé", Severity.Success);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Erreur lors de la suppression : {ex.Message}", Severity.Error);
                }
            }
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