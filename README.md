# 🛍️ iOStore — Apple Premium Reseller Argentina

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC-blue)
![Entity Framework](https://img.shields.io/badge/Entity_Framework-Core_9-green)
![SQL Server](https://img.shields.io/badge/SQL_Server-iOStoreDbase1-CC2927?logo=microsoftsqlserver)
![Deploy](https://img.shields.io/badge/Deploy-Somee.com-orange)
![Estado](https://img.shields.io/badge/Estado-En_desarrollo-yellow)

iOStore es un e-commerce de productos Apple desarrollado en ASP.NET Core 9.0 MVC. Permite a los clientes explorar el catálogo, agregar productos al carrito y realizar pedidos con múltiples métodos de pago y divisas (ARS / USD). Incluye un panel de administración completo con dashboard de ventas, gestión de usuarios, seguimiento de pedidos y notificaciones por email.

---

## 📋 Tabla de contenidos

- [Características principales](#-características-principales)
- [Tecnologías utilizadas](#-tecnologías-utilizadas)
- [Requisitos previos](#-requisitos-previos)
- [Instalación y configuración local](#-instalación-y-configuración-local)
- [Estructura del proyecto](#-estructura-del-proyecto)
- [Cuentas de acceso para pruebas](#-cuentas-de-acceso-para-pruebas-seed)
- [Variables de configuración requeridas](#-variables-de-configuración-requeridas)
- [Capturas de pantalla](#-capturas-de-pantalla)
- [Estado del proyecto y roadmap](#-estado-del-proyecto-y-roadmap)
- [Autor](#-autor)
- [Licencia](#-licencia)

---

## ✨ Características principales

### 🔐 Autenticación y usuarios
- Registro de clientes con confirmación de email (código de 6 dígitos)
- Login con bloqueo por intentos fallidos (5 intentos → 15 min)
- Recuperación de contraseña mediante código enviado por email
- Tres roles con permisos diferenciados:
  - **Administrador** — acceso total, gestión de usuarios y configuración
  - **AdminEmpleado** — gestión de pedidos, productos, categorías y dashboard
  - **Cliente** — catálogo, carrito y seguimiento de sus pedidos
- Activación / desactivación de cuentas con invalidación inmediata de sesión
- Cambio de rol con revocación automática de acceso al rol anterior
- Perfil editable por el propio usuario

### 🛒 Catálogo y carrito
- Catálogo de productos Apple: iPhone, iPad, Mac, Apple Watch, AirPods, Accesorios
- Búsqueda, filtros por categoría y paginación
- Galería de imágenes por producto
- Precios en USD con cotización en tiempo real (blue y tarjeta)
- Promociones configurables por método de pago
- Carrito persistente por usuario con contador en tiempo real
- Gestión de cantidades y eliminación de ítems

### 📦 Pedidos y seguimiento
- Checkout con cálculo automático de costo de envío por código postal
- Soporte para envío gratuito desde un monto configurable
- Múltiples métodos de pago: efectivo ARS, dólares billete (cara grande / cara chica), tarjeta con cuotas
- Pago en USD con cotización aplicada y conversión a ARS
- Recargo financiero por cuotas
- Estado del pedido con máquina de estados completa:
  `Pendiente → En trámite → Preparando → Despachado → En camino → Entregado`
  (y ramales: `Solicita devolución → En devolución → Devuelto`, `Cancelado`)
- Trazabilidad completa con historial de movimientos y empleado responsable
- Seguimiento público por número de seguimiento (sin login)
- Contacto interno por pedido entre empleados

### 📊 Panel de administración
- **Dashboard** con KPIs: ventas del período, pedidos, clientes, stock bajo
- Gráfico de ventas por día (Chart.js)
- Top productos más vendidos (stored procedure)
- Estadísticas por método de pago y divisa
- Tabla de antigüedad de empleados
- Alertas de stock bajo (≤ 5 unidades)
- Exportación del dashboard completo a Excel (ClosedXML, 7 hojas)
- Selector de rango de fechas (hoy / semana / mes / año)

### 👥 Gestión de usuarios (solo Administrador)
- Listado con búsqueda, filtro por rol y paginación
- Creación de usuarios empleados/admin desde el panel
- Edición de datos y cambio de rol (Cliente ↔ AdminEmpleado)
- Toggle Activar / Desactivar con badge de estado visible
- Reset de contraseña por administrador
- El rol Administrador está protegido: no puede asignarse ni removerse desde la interfaz

### 📧 Notificaciones por email
- Confirmación de pedido con detalle completo
- Actualización de estado automática al cambiar el pedido
- Email de despacho con orden de compra adjunta en PDF (QuestPDF)
- Email "En camino" con boleta de pago en PDF
- SMTP configurable desde la base de datos (panel Admin → Notificaciones)
- Email de prueba desde el panel
- Footer con dirección de soporte y aviso de no-reply

### ⚙️ Configuración administrativa
- Tarifas de envío por transportista y código postal
- Métodos de pago con recargo por cuotas configurable
- Configuración SMTP (host, puerto, usuario, SSL)
- Datos de cotización de dólar con fallback configurable

---

## 🧰 Tecnologías utilizadas

### Backend

| Tecnología | Versión | Uso |
|---|---|---|
| ASP.NET Core MVC | 9.0 | Framework principal |
| Entity Framework Core | 9.0.9 | ORM y migraciones |
| ASP.NET Core Identity | 9.0.9 | Autenticación y roles |
| MailKit | 4.16.0 | SMTP para notificaciones de pedidos |
| System.Net.Mail | (built-in) | SMTP para confirmación de cuenta |
| QuestPDF | 2025.4.0 | Generación de órdenes de compra en PDF |
| ClosedXML | 0.105.0 | Exportación de datos a Excel |
| Serilog.AspNetCore | 8.0.3 | Logging rotativo a consola y archivo |

### Frontend

| Tecnología | Versión | Uso |
|---|---|---|
| Bootstrap (Bootswatch Flatly) | 5.3.3 | Sistema de grilla y componentes |
| Bootstrap Icons | 1.11.3 | Iconografía |
| jQuery | 3.7.1 | Interactividad y AJAX |
| DataTables | 1.13.7 | Tablas con búsqueda y paginación |
| Chart.js | 4.4.0 | Gráficos del dashboard |

### Base de datos y deploy

| Tecnología | Detalle |
|---|---|
| SQL Server | Base de datos: `iOStoreDbase1` |
| Stored Procedures | Top productos vendidos, estadísticas de pago y envío |
| Somee.com | Hosting con SQL Server en la nube |

### Herramientas de desarrollo

- Visual Studio 2022 / VS Code
- Claude Code (asistente de desarrollo)
- EF Core Migrations (control de versiones del esquema)

---

## 📋 Requisitos previos

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0) o superior
- SQL Server 2019+ (o SQL Server Express)
- Cuenta Gmail con **App Password** habilitada (Autenticación en dos pasos requerida)
- Visual Studio 2022 o VS Code con extensión C#

---

## ⚙️ Instalación y configuración local

### 1. Clonar el repositorio

```bash
git clone https://github.com/JLitte/iOStore.git
cd iOStore
```

### 2. Configurar `appsettings.Development.json`

Este archivo **no está incluido en el repositorio** (excluido por `.gitignore`). Crealo en la raíz del proyecto con la siguiente estructura y completá con tus valores reales:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=iOStoreDbase1;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  },
  "EmailSettings": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Usuario": "tu_email@gmail.com",
    "Password": "tu_app_password_de_google",
    "Remitente": "iOStore"
  }
}
```

> Los valores reales de desarrollo están guardados en `Claves_iOStore.txt` (archivo local, no subir al repositorio).

### 3. Aplicar migraciones a la base de datos

```bash
dotnet ef database update
```

Esto crea la base de datos `iOStoreDbase1` y ejecuta todas las migraciones, incluyendo la creación de roles, stored procedures e índices. También ejecuta el **seed automático** de los usuarios iniciales (ver sección [Cuentas de acceso](#-cuentas-de-acceso-para-pruebas-seed)).

### 4. Ejecutar el proyecto

```bash
dotnet run
```

La aplicación queda disponible en `https://localhost:5001` (o el puerto que indique la consola).

---

## 🗂️ Estructura del proyecto

```
iOStore/
├── Areas/
│   ├── Admin/
│   │   ├── Controllers/        # Controladores del panel admin
│   │   │   ├── CategoriasController.cs
│   │   │   ├── ConfiguracionController.cs
│   │   │   ├── DashboardController.cs
│   │   │   ├── MetodosPagoController.cs
│   │   │   ├── TarifasController.cs
│   │   │   └── UsuariosController.cs
│   │   └── Views/              # Vistas del panel admin
│   └── Identity/               # Páginas de Identity (Razor Pages)
├── Controllers/                # Controladores públicos
│   ├── AccountController.cs    # Login, registro, perfil, recuperación de contraseña
│   ├── CarritoController.cs    # Carrito de compras
│   ├── HomeController.cs       # Inicio y home
│   ├── PedidoController.cs     # Checkout y gestión de pedidos
│   ├── ProductoController.cs   # Catálogo y ABM de productos
│   └── SeguimientoController.cs # Seguimiento público de pedidos
├── Data/
│   ├── ApplicationDbContext.cs # DbContext con todas las entidades
│   └── Migrations/             # Migraciones EF Core (m1 → m9)
├── Helpers/                    # Utilidades y constantes
│   ├── Roles.cs                # Constantes de roles del sistema
│   ├── FormatHelper.cs         # Formateo de estados y montos
│   ├── ArClock.cs              # Reloj en zona horaria Argentina
│   └── ...
├── Models/                     # Entidades del dominio
│   ├── ApplicationUser.cs      # Usuario extendido de IdentityUser
│   ├── Pedido.cs               # Pedido con estado y trazabilidad
│   ├── Producto.cs             # Producto con galería e imágenes
│   ├── CarritoItem.cs          # Ítem del carrito
│   └── ...
├── Services/                   # Capa de servicios
│   ├── SmtpEmailSender.cs      # Envío de email (confirmación de cuenta)
│   ├── NotificacionService.cs  # Notificaciones automáticas de pedidos
│   ├── CotizacionService.cs    # Cotización USD en tiempo real
│   ├── FacturaService.cs       # Generación de PDF con QuestPDF
│   ├── PedidoService.cs        # Lógica de negocio de pedidos
│   ├── EnvioService.cs         # Cálculo de costo de envío
│   └── ...
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml      # Layout público (navbar + footer Apple)
│   │   └── _Layout.Admin.cshtml # Layout admin (sidebar)
│   └── ...                     # Vistas por controlador
├── wwwroot/                    # Archivos estáticos (CSS, JS, imágenes)
├── appsettings.json            # Configuración con placeholders (sin claves reales)
├── appsettings.Development.json # ⚠️ No incluido en repo — valores reales locales
├── .env.example                # Plantilla de variables de entorno
├── .gitignore
└── Program.cs                  # Configuración de servicios, Identity y seed
```

---

## 👤 Cuentas de acceso para pruebas (seed)

Las siguientes cuentas se crean automáticamente al ejecutar `dotnet ef database update` en un entorno vacío:

| Rol | Email | Contraseña |
|---|---|---|
| Administrador | `admin@iostore.com` | Ver `Claves_iOStore.txt` |
| AdminEmpleado | `empleado@iostore.com` | Ver `Claves_iOStore.txt` |
| Cliente | Se registra desde la app | — |

> ⚠️ **Nota:** Estas credenciales son válidas únicamente en entorno local tras la ejecución del seed. Las contraseñas reales se encuentran en el archivo `Claves_iOStore.txt`, que es local y está excluido del repositorio.

---

## 🔑 Variables de configuración requeridas

Todas las variables deben estar presentes en `appsettings.Development.json` para el entorno local, o como variables de entorno en producción.

| Variable | Descripción | Cómo obtenerla |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a SQL Server | Configurar servidor SQL local o remoto |
| `EmailSettings:Host` | Servidor SMTP | `smtp.gmail.com` para Gmail |
| `EmailSettings:Port` | Puerto SMTP | `587` (STARTTLS) |
| `EmailSettings:Usuario` | Email remitente | Tu cuenta de Gmail |
| `EmailSettings:Password` | Contraseña de aplicación | [Google → Seguridad → Contraseñas de aplicación](https://myaccount.google.com/apppasswords) |
| `EmailSettings:Remitente` | Nombre visible del remitente | Ej: `iOStore` |
| `CotizacionFallback:DolarBlue` | Cotización blue de respaldo | Valor numérico en ARS |
| `CotizacionFallback:DolarTarjeta` | Cotización tarjeta de respaldo | Valor numérico en ARS |

> Las configuraciones SMTP para **notificaciones de pedidos** (estado, despacho, etc.) se gestionan desde el panel Admin → Notificaciones y se guardan en la base de datos.

---

## 📸 Capturas de pantalla

> 🖼️ *Sección pendiente — agregar screenshots del catálogo, carrito, checkout, panel admin y dashboard.*

---

## 🗺️ Estado del proyecto y roadmap

**Estado actual:** En desarrollo activo — funcionalidad core completa, en etapa de pruebas y refinamiento.

### Funcionalidades completadas
- ✅ Autenticación completa con confirmación de email y recuperación de contraseña
- ✅ Catálogo con búsqueda, filtros y galería de imágenes
- ✅ Carrito de compras persistente
- ✅ Checkout con múltiples métodos de pago y divisas
- ✅ Máquina de estados para pedidos con trazabilidad
- ✅ Panel de administración con dashboard, KPIs y gráficos
- ✅ Gestión de usuarios con roles y activación/desactivación
- ✅ Notificaciones por email automáticas con adjuntos PDF
- ✅ Exportación de datos a Excel
- ✅ Cotización USD en tiempo real con fallback
- ✅ Seguridad: claves sensibles fuera del repositorio, .gitignore configurado

### Posibles mejoras futuras
- 🔲 Términos y condiciones / Centro de ayuda (páginas placeholder en el footer)
- 🔲 Sistema de reseñas y calificaciones de productos
- 🔲 Integración con pasarela de pago online (Mercado Pago / Stripe)
- 🔲 Panel de estadísticas para el cliente (historial de compras con gráficos)
- 🔲 Notificaciones push o WhatsApp
- 🔲 API REST para integración con apps móviles

---

## 👨‍💻 Autor

**Juan Litterini**
- GitHub: [@JLitte](https://github.com/JLitte)
- Rol: Desarrollador - Estudiante
- I.T.E.S - Santa Rosa, La Pampa.

---

## 📄 Licencia

© 2026 Juan Litterini — Todos los derechos reservados.

```
© 2026 Juan Litterini — Todos los derechos reservados.
Se permite el uso, copia, modificación y distribución de este software
con o sin restricciones, sujeto a que se incluya este aviso de copyright.
```
