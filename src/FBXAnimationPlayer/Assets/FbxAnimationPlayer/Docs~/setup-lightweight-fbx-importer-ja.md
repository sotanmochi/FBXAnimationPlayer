# Lightweight FBX Importer のセットアップ

## パッケージのインストール
<img src="./LightweightFBXImporter.png" height="480">

<img src="./LightweightFBXImporterPackage.png" height="480">

## ネイティブプラグイン構成
Lightweight FBX Importer は [ufbx](https://github.com/ufbx/ufbx) というライブラリを使用してFBXファイルを読み込む。
プラットフォームごとのネイティブプラグイン構成は以下のようになっている。

| プラットフォーム | 使用されるファイル | 種別 |
|---|---|---|
| Windows | `Plugins/Windows/*/ufbx.dll` | プリビルド |
| macOS | `Plugins/OSX/libufbx.dylib` | プリビルド |
| Linux | `Plugins/Linux/*/libufbx.so` | プリビルド |
| Android | `Plugins/Android/*/libufbx.so` | プリビルド |
| iOS | `Plugins/Source/ufbx.c` | コードをXcodeでコンパイル |
| WebGL | `Plugins/Source/ufbx.c` | コードをEmscriptenでコンパイル |

## iOS ビルドのためのコード修正
Lightweight FBX Importer を iOS 用のビルドで正常に動作させるためには、
インポートしたパッケージに含まれる `ufbx.c` を手動で修正する必要がある。修正を加えないと**FBX ファイルの読み込みが常に失敗する**。

`ufbx.c` の最初の数行は以下のようなコードになっている（ufbx のバージョンによって多少異なる場合がある）。

```c
#ifndef UFBX_UFBX_C_INCLUDED
#define UFBX_UFBX_C_INCLUDED

#if defined(UFBX_HEADER_PATH)
    #include UFBX_HEADER_PATH
#else
    #include "ufbx.h"
#endif

...
```

`#include "ufbx.h"` よりも前に `#define UFBX_REAL_IS_FLOAT` を追加する。

```c
#ifndef UFBX_UFBX_C_INCLUDED
#define UFBX_UFBX_C_INCLUDED

#define UFBX_REAL_IS_FLOAT // これを追加する

#if defined(UFBX_HEADER_PATH)
    #include UFBX_HEADER_PATH
#else
    #include "ufbx.h"
#endif

...
```

この修正により、`ufbx_real` が `float`（4バイト）として定義されて C# のラッパー DLL と構造体のレイアウトが一致するようになり、iOS ビルドで FBX ファイルの読み込みが成功するようになる。
