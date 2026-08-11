# Plan: Modulo Todo List

## Resumen

Implementar un CRUD completo de Todo List como primer modulo del Toolbox. Todos los endpoints requieren JWT. Cada usuario solo ve y gestiona sus propios todos. Cada TodoItem puede tener archivos multimedia adjuntos (imagenes, videos y audio) almacenados en disco con metadata en base de datos.

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `src/Toolbox.Core/Entities/TodoItem.cs` | Agregar Description, DueDate, Priority, IsCompleted y coleccion de Attachments |
| `src/Toolbox.Core/Enums/FileType.cs` | Agregar `Audio = 2` |
| `src/Toolbox.Core/Interfaces/IRepository.cs` | Agregar metodo `FindAsync` para queries filtradas |
| `src/Toolbox.Infrastructure/Repositories/GenericRepository.cs` | Implementar `FindAsync` |
| `src/Toolbox.Infrastructure/Data/Configurations/TodoItemConfiguration.cs` | Configurar nuevas columnas y relacion con TodoAttachment |
| `src/Toolbox.Infrastructure/Data/ToolboxDbContext.cs` | Agregar `DbSet<TodoAttachment>` |
| `src/Toolbox.API/Program.cs` | Registrar ITodoService e IFileService en DI |
| `src/Toolbox.API/appsettings.json` | Agregar extensiones de audio y MaxAudioSizeMB a FileUpload |

## Archivos a crear

| Archivo | Contenido |
|---------|-----------|
| `src/Toolbox.Core/Entities/TodoAttachment.cs` | Entidad para archivos adjuntos |
| `src/Toolbox.Core/Interfaces/ITodoService.cs` | Interface del servicio de todos |
| `src/Toolbox.Core/Interfaces/IFileService.cs` | Interface del servicio de archivos |
| `src/Toolbox.Core/Services/TodoService.cs` | Implementacion CRUD de todos |
| `src/Toolbox.Infrastructure/Services/FileService.cs` | Implementacion de manejo de archivos en disco |
| `src/Toolbox.Infrastructure/Data/Configurations/TodoAttachmentConfiguration.cs` | Configuracion EF de TodoAttachment |
| `src/Toolbox.API/DTOs/Todo/CreateTodoRequest.cs` | DTO para crear todo |
| `src/Toolbox.API/DTOs/Todo/UpdateTodoRequest.cs` | DTO para actualizar todo |
| `src/Toolbox.API/DTOs/Todo/TodoItemDto.cs` | DTO de respuesta de todo |
| `src/Toolbox.API/DTOs/Todo/TodoAttachmentDto.cs` | DTO de respuesta de adjunto |
| `src/Toolbox.API/Controllers/TodoController.cs` | Endpoints REST de todos y adjuntos |
| `tests/Toolbox.Core.Tests/Services/TodoServiceTests.cs` | Tests unitarios del servicio |
| `tests/Toolbox.Api.Tests/Controllers/TodoControllerTests.cs` | Tests unitarios del controller |
| `tests/Toolbox.Integration.Tests/TodoControllerTests.cs` | Tests de integracion |

---

## 1. Agregar `FindAsync` a IRepository

**Archivo:** `src/Toolbox.Core/Interfaces/IRepository.cs`

Agregar metodo:
```csharp
Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
```

**Por que:** `GetAllAsync()` trae TODOS los registros de la tabla. Para listar los todos de un usuario necesitamos filtrar en la query. Este metodo es generico y beneficia a todos los modulos futuros.

**Implementacion en GenericRepository:**
```csharp
public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
{
    return await _dbSet.Where(predicate).ToListAsync();
}
```

---

## 2. Actualizar FileType enum

**Archivo:** `src/Toolbox.Core/Enums/FileType.cs`

```csharp
public enum FileType
{
    Image = 0,
    Video = 1,
    Audio = 2
}
```

**Por que:** El enum actual solo tiene Image y Video. Se necesita Audio para soportar archivos de sonido.

---

## 3. Entidad TodoAttachment (nueva)

**Archivo:** `src/Toolbox.Core/Entities/TodoAttachment.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Toolbox.Core.Entities;

public class TodoAttachment : BaseEntity
{
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StoredPath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public FileType FileType { get; set; }

    // Foreign key
    public Guid TodoItemId { get; set; }
    public TodoItem? TodoItem { get; set; } = null!;
}
```

Campos:
- `FileName`: Nombre original del archivo subido por el usuario
- `StoredPath`: Ruta relativa en disco donde se almacena el archivo (ej: `Uploads/todo-attachments/{guid}.jpg`)
- `ContentType`: MIME type del archivo (ej: `image/jpeg`, `video/mp4`, `audio/mpeg`)
- `FileSizeBytes`: Tamaño del archivo en bytes
- `FileType`: Enum que indica si es Image, Video o Audio

---

## 4. Expandir entidad TodoItem

**Archivo:** `src/Toolbox.Core/Entities/TodoItem.cs`

Propiedades finales:
```csharp
using System.ComponentModel.DataAnnotations;

namespace Toolbox.Core.Entities;

public class TodoItem : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsCompleted { get; set; } = false;

    public Priority Priority { get; set; } = Priority.Medium;

    public DateTime? DueDate { get; set; }

    // Foreign key a User
    public Guid UserId { get; set; }
    public User? User { get; set; } = null!;

    // Archivos adjuntos
    public ICollection<TodoAttachment> Attachments { get; set; } = new List<TodoAttachment>();
}
```

**Actualizar TodoItemConfiguration** para las nuevas columnas:
- Description: opcional, maxlength 1000
- IsCompleted: required, default false
- Priority: required, stored as int, default Medium
- DueDate: opcional

**Crear TodoAttachmentConfiguration:**
- FileName: required, maxlength 255
- StoredPath: required, maxlength 500
- ContentType: required, maxlength 100
- FileSizeBytes: required
- FileType: required, stored as int
- TodoItemId: FK a TodoItem con cascade delete

**Actualizar ToolboxDbContext:**
```csharp
public DbSet<TodoAttachment> TodoAttachments { get; set; } = null!;
```

---

## 5. Actualizar configuracion de FileUpload

**Archivo:** `src/Toolbox.API/appsettings.json`

```json
"FileUpload": {
    "MaxFileSizeMB": 10,
    "MaxVideoSizeMB": 100,
    "MaxAudioSizeMB": 50,
    "AllowedImageExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"],
    "AllowedVideoExtensions": [".mp4", ".avi", ".mov", ".webm"],
    "AllowedAudioExtensions": [".mp3", ".wav", ".ogg", ".m4a", ".aac"],
    "UploadPath": "Uploads"
}
```

Cambios respecto al actual:
- Agregar `MaxAudioSizeMB: 50`
- Agregar `AllowedAudioExtensions` con formatos de audio comunes
- Agregar `.webp` a imagenes y `.webm` a video (formatos modernos)

---

## 6. IFileService (nueva)

**Archivo:** `src/Toolbox.Core/Interfaces/IFileService.cs`

```csharp
using Microsoft.AspNetCore.Http;

namespace Toolbox.Core.Interfaces;

public interface IFileService
{
    Task<TodoAttachment> SaveFileAsync(IFormFile file, Guid todoItemId);
    Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(Guid attachmentId);
    Task<bool> DeleteFileAsync(string storedPath);
    bool IsAllowedFile(IFormFile file);
    FileType DetermineFileType(string contentType);
}
```

Notas:
- `SaveFileAsync`: Valida el archivo, lo guarda en disco y retorna la entidad `TodoAttachment` (sin persistir en DB, eso lo hace el controller/service)
- `GetFileAsync`: Lee el archivo de disco y retorna stream + metadata para descarga
- `DeleteFileAsync`: Elimina archivo fisico del disco
- `IsAllowedFile`: Valida extension y tamaño segun configuracion
- `DetermineFileType`: Mapea content type a enum FileType

---

## 7. FileService (nueva)

**Archivo:** `src/Toolbox.Infrastructure/Services/FileService.cs`

Logica:
- Lee configuracion de `FileUpload` desde `IConfiguration`
- `SaveFileAsync`:
  1. Valida que el archivo sea permitido (extension + tamaño)
  2. Determina el `FileType` basado en el content type
  3. Genera nombre unico: `{Guid}{extension}`
  4. Crea subdirectorio `Uploads/todo-attachments/` si no existe
  5. Copia el stream al disco
  6. Retorna entidad `TodoAttachment` con todos los campos llenos
- `GetFileAsync`: Abre el archivo como `FileStream` y retorna tupla
- `DeleteFileAsync`: Usa `File.Delete` si el archivo existe
- `IsAllowedFile`: Checa extension contra listas permitidas y tamaño contra limites por tipo
- `DetermineFileType`:
  - `image/*` → `FileType.Image`
  - `video/*` → `FileType.Video`
  - `audio/*` → `FileType.Audio`

---

## 8. ITodoService

**Archivo:** `src/Toolbox.Core/Interfaces/ITodoService.cs`

```csharp
public interface ITodoService
{
    Task<IEnumerable<TodoItem>> GetAllByUserAsync(Guid userId);
    Task<TodoItem?> GetByIdAsync(Guid todoId, Guid userId);
    Task<TodoItem> CreateAsync(TodoItem todoItem);
    Task<(bool Success, string? Error)> UpdateAsync(TodoItem todoItem, Guid userId);
    Task<(bool Success, string? Error)> DeleteAsync(Guid todoId, Guid userId);
    Task<TodoAttachment> AddAttachmentAsync(Guid todoId, Guid userId, TodoAttachment attachment);
    Task<(bool Success, string? Error)> RemoveAttachmentAsync(Guid attachmentId, Guid todoId, Guid userId);
    Task<IEnumerable<TodoAttachment>> GetAttachmentsAsync(Guid todoId, Guid userId);
}
```

Notas:
- Todos los metodos que acceden a un todo especifico reciben `userId` para verificar ownership
- `GetAllByUserAsync` usa el nuevo `FindAsync` del repositorio
- `UpdateAsync`/`DeleteAsync` retornan tupla con error para diferenciar "no encontrado" de "no autorizado"
- `AddAttachmentAsync`: Agrega un adjunto a un todo (verifica ownership del todo)
- `RemoveAttachmentAsync`: Elimina un adjunto de un todo (verifica ownership)
- `GetAttachmentsAsync`: Lista adjuntos de un todo (verifica ownership)

---

## 9. TodoService

**Archivo:** `src/Toolbox.Core/Services/TodoService.cs`

Logica:
- `GetAllByUserAsync`: `_todoRepo.FindAsync(t => t.UserId == userId)` (incluir Attachments con eager loading)
- `GetByIdAsync`: `_todoRepo.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId)` (incluir Attachments)
- `CreateAsync`: Asigna UserId y llama `_todoRepo.AddAsync`
- `UpdateAsync`: Busca el todo verificando ownership, actualiza campos, llama `_todoRepo.UpdateAsync`
- `DeleteAsync`: Busca el todo verificando ownership, llama `_todoRepo.DeleteAsync` (cascade borra adjuntos en DB, el controller se encarga de borrar archivos fisicos via IFileService)
- `AddAttachmentAsync`: Verifica ownership del todo, llama `_attachmentRepo.AddAsync`
- `RemoveAttachmentAsync`: Verifica ownership del todo, busca el adjunto, llama `_attachmentRepo.DeleteAsync`, retorna StoredPath para que el controller borre el archivo fisico
- `GetAttachmentsAsync`: Verifica ownership del todo, retorna `_attachmentRepo.FindAsync(a => a.TodoItemId == todoId)`

**Dependencias:** `IRepository<TodoItem>`, `IRepository<TodoAttachment>`

---

## 10. DTOs

**CreateTodoRequest:**
- Title: `[Required]`, `[MaxLength(200)]`
- Description: `[MaxLength(1000)]`, opcional
- Priority: int (0-2), default Medium
- DueDate: DateTime?, opcional

**UpdateTodoRequest:**
- Mismos campos que Create + `IsCompleted` (bool)

**TodoAttachmentDto** (respuesta):
- Id, FileName, ContentType, FileSizeBytes, FileType, CreatedAt
- DownloadUrl: string (generada por el controller, ej: `/api/todo/{todoId}/attachments/{attachmentId}/download`)

**TodoItemDto** (respuesta):
- Id, Title, Description, IsCompleted, Priority, DueDate, CreatedAt, UpdatedAt
- Attachments: `List<TodoAttachmentDto>` (lista de adjuntos del todo)

---

## 11. TodoController

**Archivo:** `src/Toolbox.API/Controllers/TodoController.cs`

Ruta base: `api/todo`. Todos los endpoints con `[Authorize]`.

### Endpoints CRUD de Todos

| Metodo | Ruta | Descripcion |
|--------|------|-------------|
| GET | `/api/todo` | Listar todos del usuario |
| GET | `/api/todo/{id}` | Obtener un todo por ID (incluye adjuntos) |
| POST | `/api/todo` | Crear nuevo todo |
| PUT | `/api/todo/{id}` | Actualizar todo |
| DELETE | `/api/todo/{id}` | Eliminar todo (y sus adjuntos fisicos) |

### Endpoints de Adjuntos Multimedia

| Metodo | Ruta | Descripcion |
|--------|------|-------------|
| POST | `/api/todo/{todoId}/attachments` | Subir archivo(s) adjunto(s) |
| GET | `/api/todo/{todoId}/attachments` | Listar adjuntos de un todo |
| GET | `/api/todo/{todoId}/attachments/{attachmentId}/download` | Descargar un adjunto |
| DELETE | `/api/todo/{todoId}/attachments/{attachmentId}` | Eliminar un adjunto |

### Detalles de endpoints de adjuntos

**POST `/api/todo/{todoId}/attachments`**
- Acepta `[FromForm] List<IFormFile> files`
- Valida cada archivo con `IFileService.IsAllowedFile()`
- Guarda en disco con `IFileService.SaveFileAsync()`
- Persiste metadata con `ITodoService.AddAttachmentAsync()`
- Retorna `201 Created` con lista de `TodoAttachmentDto`
- Limitar a maximo 5 archivos por request

**GET `/api/todo/{todoId}/attachments/{attachmentId}/download`**
- Busca el adjunto verificando ownership del todo
- Usa `IFileService.GetFileAsync()` para obtener stream
- Retorna `FileStreamResult` con content type correcto

**DELETE `/api/todo/{todoId}/attachments/{attachmentId}`**
- Verifica ownership del todo
- Elimina de DB con `ITodoService.RemoveAttachmentAsync()`
- Elimina archivo fisico con `IFileService.DeleteFileAsync()`

**DELETE `/api/todo/{id}` (actualizado)**
- Al eliminar un todo, tambien elimina los archivos fisicos de todos sus adjuntos
- Obtiene lista de adjuntos antes de borrar el todo
- Itera y llama `IFileService.DeleteFileAsync()` por cada uno
- Luego borra el todo (cascade elimina registros en DB)

Helper privado para extraer userId del JWT:
```csharp
private Guid? GetUserId()
{
    var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Guid.TryParse(claim, out var id) ? id : null;
}
```

---

## 12. Registro DI

**Archivo:** `src/Toolbox.API/Program.cs`

Agregar despues de la linea de `IAuthService`:
```csharp
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IFileService, FileService>();
```

---

## 13. Migracion EF Core

Generar migracion para los cambios en TodoItem y la nueva tabla TodoAttachments:
```bash
dotnet ef migrations add ExpandTodoItemWithAttachments --project src/Toolbox.Infrastructure --startup-project src/Toolbox.API
```

---

## 14. Tests

### Tests unitarios del servicio (Toolbox.Core.Tests)

**TodoService:**
- `GetAllByUser_Should_Return_Only_User_Todos`
- `GetById_Should_Return_Null_For_Other_Users_Todo`
- `Create_Should_Set_UserId_And_Save`
- `Update_Should_Fail_For_Nonexistent_Todo`
- `Delete_Should_Fail_For_Other_Users_Todo`
- `AddAttachment_Should_Fail_For_Other_Users_Todo`
- `AddAttachment_Should_Save_Attachment_To_Repository`
- `RemoveAttachment_Should_Fail_For_Nonexistent_Attachment`
- `GetAttachments_Should_Return_Attachments_For_Owned_Todo`

### Tests unitarios del controller (Toolbox.Api.Tests)

**TodoController CRUD:**
- `GetAll_Should_Return_Ok_With_TodoList`
- `GetById_Should_Return_Ok_When_Found`
- `GetById_Should_Return_NotFound`
- `Create_Should_Return_Created`
- `Update_Should_Return_NoContent_On_Success`
- `Update_Should_Return_NotFound`
- `Delete_Should_Return_NoContent_On_Success`
- `Delete_Should_Return_NotFound`
- `All_Endpoints_Should_Return_Unauthorized_Without_Claim`

**TodoController Attachments:**
- `UploadAttachment_Should_Return_Created_With_AttachmentDto`
- `UploadAttachment_Should_Return_BadRequest_For_Invalid_File`
- `UploadAttachment_Should_Return_NotFound_For_Other_Users_Todo`
- `DownloadAttachment_Should_Return_FileStream`
- `DownloadAttachment_Should_Return_NotFound_For_Missing_File`
- `DeleteAttachment_Should_Return_NoContent_On_Success`
- `ListAttachments_Should_Return_Ok_With_AttachmentList`

### Tests de integracion (Toolbox.Integration.Tests)

**CRUD:**
- `Create_Should_Return_Created_Todo`
- `GetAll_Should_Return_Only_Own_Todos`
- `GetById_Should_Return_Todo_With_Attachments`
- `Update_Should_Modify_Todo`
- `Delete_Should_Remove_Todo_And_Attachments`
- `Endpoints_Should_Return_Unauthorized_Without_Token`

**Attachments:**
- `Upload_Image_Should_Return_Created_Attachment`
- `Upload_Video_Should_Return_Created_Attachment`
- `Upload_Audio_Should_Return_Created_Attachment`
- `Upload_Invalid_Extension_Should_Return_BadRequest`
- `Upload_Oversized_File_Should_Return_BadRequest`
- `Download_Attachment_Should_Return_File_Content`
- `Delete_Attachment_Should_Remove_File_And_Record`
- `Delete_Todo_Should_Cascade_Delete_Attachment_Files`
- `Cannot_Access_Other_Users_Attachments`

---

## Orden de implementacion

1. `FindAsync` en IRepository + GenericRepository
2. Actualizar FileType enum (agregar Audio)
3. Crear entidad TodoAttachment
4. Expandir TodoItem entity (agregar campos + coleccion Attachments)
5. Actualizar TodoItemConfiguration
6. Crear TodoAttachmentConfiguration
7. Agregar DbSet<TodoAttachment> en ToolboxDbContext
8. Actualizar appsettings.json (agregar config de audio)
9. IFileService + FileService
10. ITodoService + TodoService
11. DTOs (Create, Update, TodoItemDto, TodoAttachmentDto)
12. TodoController (CRUD + endpoints de adjuntos)
13. Registrar ITodoService e IFileService en Program.cs
14. Generar migracion EF
15. Tests unitarios del servicio
16. Tests unitarios del controller
17. Tests de integracion

## Verificacion

```bash
cd backend
dotnet build
dotnet test
dotnet ef migrations add ExpandTodoItemWithAttachments --project src/Toolbox.Infrastructure --startup-project src/Toolbox.API
```

Todos los tests existentes (30) + nuevos deben pasar. Probar manualmente con Swagger que:
- Los endpoints CRUD de todos funcionan con JWT
- Se pueden subir imagenes (jpg, png, gif, webp), videos (mp4, avi, mov, webm) y audios (mp3, wav, ogg, m4a, aac)
- Se pueden descargar los archivos subidos
- Al eliminar un todo se eliminan sus archivos fisicos
- Se rechazan archivos con extensiones no permitidas o que excedan el tamaño maximo
- Un usuario no puede acceder a adjuntos de todos de otro usuario
