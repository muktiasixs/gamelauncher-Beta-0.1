<div align="center">
  <h1> <img src="launchergame.ico" width="30" height="30"> MyGameLauncher</h1> 
  <p><b>Pengelola dan Peluncur Game Otomatis Berbasis Desktop</b></p>
  <p>
    <img src="https://img.shields.io/badge/Status-Beta_v0.1-orange.svg" alt="Status" />
    <img src="https://img.shields.io/badge/.NET-9.0-512BD4.svg?logo=dotnet" alt=".NET 9.0" />
    <img src="https://img.shields.io/badge/Platform-Windows-0078D6.svg?logo=windows" alt="Windows" />
  </p>
</div>

**MyGameLauncher** adalah aplikasi desktop modern, ringan, dan cepat yang dirancang untuk membantu Anda mengorganisir dan meluncurkan koleksi game PC dari satu tempat. Dibangun menggunakan **C#** dan **.NET 9 Windows Forms**, aplikasi ini menonjolkan otomatisasi penuh dalam mendeteksi dan mengelola direktori game Anda.

Aplikasi ini dikembangkan sebagai bagian dari portofolio rekayasa perangkat lunak (Software Engineering) untuk menunjukkan implementasi *File System Monitoring*, pengelolaan memori (GDI+), dan UI/UX interaktif pada aplikasi Desktop.

---

## Fitur Unggulan

- **Smart Auto-Discovery**: Secara otomatis memindai dan menemukan game (file `.exe`, `.lnk`, `.url`), sembari menyaring file sistem atau redistributable (*setup, vc_redist, unins, crash, dll*).
- **Real-time Folder Monitoring**: Memanfaatkan `FileSystemWatcher` untuk mendeteksi penambahan, penghapusan, atau perubahan nama file game secara *real-time* tanpa perlu *refresh* manual.
- **Pencarian Instan (Live Search)**: Temukan game favorit Anda dalam hitungan detik dengan fitur pencarian interaktif.
- **Kustomisasi Tampilan**: Dukungan penuh untuk mengganti gambar *background* sesuai selera Anda dengan penyesuaian skala otomatis (Auto-Scale & Color Matrix).
- **Informasi Detail File**: Klik pada game untuk melihat informasi instan termasuk ikon resolusi tinggi, ukuran file, tipe ekstensi, dan direktori penyimpanan.

---

## Mulai Menggunakan (Getting Started)

### Prasyarat
- OS: Windows 10 / Windows 11
- Runtime: .NET 9.0 Desktop Runtime

### Cara Instalasi & Penggunaan
1. Unduh rilis terbaru (atau *clone* repositori dan lakukan *build* via Visual Studio 2022).
2. Jalankan `GameLauncher_BETA.exe`.
3. Klik tombol **Set Folder** di bagian atas untuk memilih direktori tempat game Anda terinstal.
4. Aplikasi akan secara otomatis menyinkronkan daftar game Anda!

---

## Roadmap (Rencana Pengembangan)
- [ ] **Integrasi Database (SQLite)** - Menyimpan data historis dan melacak total waktu bermain (*Playtime*).
- [ ] **Sistem "Favorites"** - Fitur pin/bintang untuk game yang paling sering dimainkan.
- [ ] **Kustomisasi Metadata** - Kemampuan pengguna untuk mengubah nama judul dan ikon game secara manual.
- [ ] **Auto-Update System** - Mengunduh pembaruan aplikasi langsung dari website resmi.

---

## ⚠️ Catatan Rilis (Disclaimer)
Aplikasi ini saat ini berstatus **BETA**. Fitur utama aplikasi sudah berjalan stabil. Namun, Anda mungkin masih akan menemui batasan performa terkait ekstraksi ikon dari *shortcut* tertentu atau penggunaan memori (RAM) saat menggunakan gambar *background* beresolusi sangat tinggi (4K+). Pembaruan untuk optimasi *resource* akan hadir di *patch* mendatang.
