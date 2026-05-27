namespace iOStore.Services
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile image, string folder = "productos");
        void DeleteImage(string? imagePath);
        bool IsValidImage(IFormFile file);
    }

    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> SaveImageAsync(IFormFile image, string folder = "productos")
        {
            try
            {
                if (image == null || image.Length == 0)
                    throw new ArgumentException("Archivo de imagen inválido");

                if (!IsValidImage(image))
                    throw new ArgumentException("Tipo de archivo no permitido");

                // Crear carpeta si no existe
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", folder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generar nombre único
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Guardar archivo
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Retornar ruta relativa
                return $"/images/{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar imagen");
                throw;
            }
        }

        public void DeleteImage(string? imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                    return;

                // Convertir ruta relativa a absoluta
                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"Imagen eliminada: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar imagen: {imagePath}");
            }
        }

        public bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // Verificar tamaño
            if (file.Length > _maxFileSize)
                return false;

            // Verificar extensión
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return false;

            // Verificar tipo MIME
            var mimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp" };
            if (!mimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return false;

            return true;
        }
    }
}

// Agregar al Program.cs después de AddDbContext:
// builder.Services.AddScoped<IImageService, ImageService>();