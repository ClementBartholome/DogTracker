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
        private string _selectedDocumentName;
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

        private Task LoadExistingFiles()
        {
            var uploadsPath = Path.Combine(Environment.WebRootPath, "uploads", DogId.ToString());
            if (Directory.Exists(uploadsPath))
            {
                var files = new List<GedFile>();
        
                files.AddRange(Directory.GetFiles(uploadsPath, "scan-*.jpg")
                    .Select(f => new GedFile
                    {
                        PreviewUrl = $"/uploads/{DogId}/{Path.GetFileName(f)}",
                        DownloadUrl = $"/uploads/{DogId}/{Path.GetFileName(f)}",
                        Category = Enum.Parse<TypeFilesEnum>(GetCategoryFromFileName(Path.GetFileName(f))),
                        CreatedAt = File.GetCreationTime(f),
                        Name = GetNameFromFileName(Path.GetFileName(f))
                    }));
        
                files.AddRange(Directory.GetFiles(uploadsPath, "upload-*")
                    .Select(f => {
                        var fileName = Path.GetFileName(f);
                        var fileExt = Path.GetExtension(f).ToLowerInvariant();
                        var isImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }.Contains(fileExt);
                
                        return new GedFile
                        {
                            PreviewUrl = isImage 
                                ? $"/uploads/{DogId}/{fileName}" 
                                : GetIconForFileType(fileExt),
                            DownloadUrl = $"/uploads/{DogId}/{fileName}",
                            Category = Enum.Parse<TypeFilesEnum>(GetCategoryFromFileName(fileName)),
                            CreatedAt = File.GetCreationTime(f),
                            Name = GetNameFromFileName(fileName)
                        };
                    }));
        
                scannedFiles = files.OrderByDescending(f => f.CreatedAt).ToList();
            }

            return Task.CompletedTask;
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
            return parts.Length >= 4 ? parts[2] : "Divers";
        }
        
        private string GetNameFromFileName(string fileName)
        {
            var parts = fileName.Split('-');
            return parts.Length >= 4 ? parts[1] : fileName;
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
            if (string.IsNullOrEmpty(previewImage) || _selectedCategory == null || string.IsNullOrEmpty(_selectedDocumentName))
                return;

            var base64Data = previewImage.Split(',')[1];
            var imageBytes = Convert.FromBase64String(base64Data);

            var uploadsPath = Path.Combine(Environment.WebRootPath, "uploads", DogId.ToString());
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"scan-{_selectedDocumentName}-{_selectedCategory}-{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var filePath = Path.Combine(uploadsPath, fileName);

            await File.WriteAllBytesAsync(filePath, imageBytes);

            var newFile = new GedFile
            {
                Name = _selectedDocumentName,
                PreviewUrl = $"/uploads/{DogId}/{fileName}",
                DownloadUrl = $"/uploads/{DogId}/{fileName}",
                Category = _selectedCategory,
                CreatedAt = DateTime.Now
            };

            scannedFiles.Insert(0, newFile);
    
            Snackbar.Add("Document enregistré avec succès", Severity.Success);

            previewImage = null;
            isCaptureMode = false;
            _selectedDocumentName = null;
            await StopCamera();
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
        
                // You might need to create these icon images or use Material Icons
                previewImage = iconPath;
            }
    
            isCaptureMode = true; // Switch to preview mode
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
            if (_files.Count == 0 || _selectedCategory == null || string.IsNullOrEmpty(_selectedDocumentName))
                return;
    
            var file = _files[0];
            var extension = Path.GetExtension(file.Name);
            if (string.IsNullOrEmpty(extension))
                extension = ".bin";
    
            var uploadsPath = Path.Combine(Environment.WebRootPath, "uploads", DogId.ToString());
            Directory.CreateDirectory(uploadsPath);
    
            var fileName = $"upload-{_selectedDocumentName}-{_selectedCategory}-{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);
    
            using (var stream = file.OpenReadStream(maxAllowedSize: 10485760)) // 10MB
            using (var fileStream = new FileStream(filePath, FileMode.Create))
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