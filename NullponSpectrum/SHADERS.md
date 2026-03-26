# カスタムシェーダを Beat Saber で使う手順

NullponSpectrum は起動時に **`nullponspectrum_shaders`**（拡張子なし）の **AssetBundle** を読み込みます（`ShaderBundleLoader.cs`）。

**優先順:** `Beat Saber/Plugins/` に同名ファイルがあれば **そちら**（開発用の上書き）、無ければ **DLL に埋め込んだバンドル**（`LoadFromMemory`）。どちらも無い場合はカスタムシェーダだけスキップします。

中に含まれたシェーダは `Resources.FindObjectsOfTypeAll<Shader>()` で見つかるようになり、`VisualizerUtil.GetShader("Custom/StageProceduralSparks")` などが成功します。

ステージ床のスペクトラム表示をシェーダー側で行う **`Custom/StageSpectrumFloor`**（`Assets/Shaders/StageSpectrumFloor.shader`）も同じバンドル `nullponspectrum_shaders` に含めてビルドしてください。含まれない場合は `Sprites/Default` にフォールバックし、従来どおり CPU で頂点色を更新します。

---

## 1. 使う Unity のバージョン

**ゲームのバージョンと同じ Unity**を使う必要があります。  
[BSMG Wiki · Useful Links / Modding](https://bsmg.wiki/) や各バージョン向け Modding ガイドの **Unity 対応表**を確認してください。

`manifest.json` の `gameVersion` と、実際にプレイする Beat Saber のバージョンに合わせ、**その版向けの Unity** を [BSMG Wiki](https://bsmg.wiki/) 等で確認します。

### Unity 6000.0.40f1（Unity 6）について

**そのバージョンでは Beat Saber 向け AssetBundle は原則として作れません（使わないでください）。**

- Beat Saber は **古い Unity（Built-in レンダパイプライン）**でビルドされており、ゲームが読めるバンドル・シェーダ形式と **Unity 6 の出力は一致しません**。
- バンドルが読めても **マゼンタ表示・クラッシュ・シェーダだけ無効**などになりやすいです。
- **手順の考え方**（バンドル名 `nullponspectrum_shaders`、`Plugins` に置く、など）は同じでも、**実行する Unity のエディタは Wiki の対応表どおりの版に限定**してください。

---

## 2. Unity プロジェクトの準備

1. Wiki で合わせた **Beat Saber 用の Unity** で **3D（Built-in レンダパイプライン）** プロジェクトを作る。
2. このリポの `Assets/Shaders/StageProceduralSparks.shader` などを、Unity 側の **`Assets`** 以下にコピーする（例: `Assets/NullponShaders/StageProceduralSparks.shader`）。

---

## 3. 「AssetBundle 名」をアセットに付ける（ビルド前に必須）

**ビルド**とは、「この名前の箱に、どのアセットを入れるか」を Unity に教えたうえで、**箱ごとファイルを書き出す**作業です。  
その「箱の名前」が **`nullponspectrum_shaders`** です（コードの `ShaderBundleLoader.BundleFileName` と **完全一致**）。

### 手順（Project でシェーダを選ぶ）

1. Unity 左下の **Project** で、入れた **`.shader` ファイル**をクリックして選択する。
2. 右の **Inspector** を一番下までスクロールする。
3. 下の方に **AssetBundle** という行がある（版によって **プレビュー下**や **ラベル付近**にある）。
   - 左のドロップダウンが **None** になっているはず。
   - **None** をクリック → **New** を選ぶ。
   - 名前を **`nullponspectrum_shaders`** と入力する（**拡張子なし**。大文字小文字は出力ファイル名と一致させる）。
   - 右の **Variant** は **None** のままでよいことが多い。
4. **Ctrl+S** で保存する。

**Material 経由でも可:** シェーダではなく **Material** に同じバンドル名 `nullponspectrum_shaders` を付けても、参照している Shader はバンドルに含まれる。

ここまでやらないと **ビルドしても中身が空**だったり **別名のファイル**になったりする。

---

## 4. AssetBundle をビルドする（何をしているか）

**意味:** `BuildPipeline.BuildAssetBundles` が、「**AssetBundle 名が付いたアセット**」だけを集めて、**フォルダにファイルとして書き出す**処理。手でファイルを編集する作業ではない。

### 手順（Editor スクリプト）

1. Unity プロジェクトで **`Assets/Editor`** フォルダを作る（名前は **`Editor` 固定**）。
2. その中に **`BuildNullponShaders.cs`** を作り、次を貼り付けて保存する。

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildNullponShaders
{
    [MenuItem("Nullpon/Build Shader Bundle (Windows64)")]
    public static void Build()
    {
        string outDir = Path.Combine(UnityEngine.Application.dataPath, "..", "ShaderBundleOut");
        Directory.CreateDirectory(outDir);
        BuildPipeline.BuildAssetBundles(
            outDir,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64);

        UnityEngine.Debug.Log($"Built to: {outDir} — copy 'nullponspectrum_shaders' into repo EmbeddedAssets/ and rebuild DLL, or copy to Beat Saber/Plugins/");
    }
}
```

3. コンパイルが終わるまで待つ。
4. メニュー **`Nullpon` → `Build Shader Bundle (Windows64)`** を実行する。
5. **Console** に `Built to: ...` が出れば成功。

### 出力物（これがビルド結果）

- プロジェクトの **`Assets` と同じ階層**に **`ShaderBundleOut`** フォルダができる。
- その中の **`nullponspectrum_shaders`**（**拡張子なし**）が、ゲームにコピーする本体。
- **`nullponspectrum_shaders.manifest`** は説明用。**Plugins には不要**。

### 別手段: Asset Bundle Browser

Package Manager で **Asset Bundle Browser** を入れて GUI からビルドしてもよい。前提は同じ（**先に AssetBundle 名を付ける**こと）。

---

## 5. MOD（DLL）に埋め込む（推奨）

1. リポジトリの **`NullponSpectrum/EmbeddedAssets/`** フォルダを作る（無ければ）。
2. Unity でビルドした **`nullponspectrum_shaders`**（拡張子なし）を、そのフォルダにコピーする。
3. **`dotnet build` / Visual Studio で NullponSpectrum を再ビルド**する。  
   - `NullponSpectrum.csproj` は **`EmbeddedAssets\nullponspectrum_shaders` が存在するときだけ** `EmbeddedResource` として取り込みます。ファイルが無い場合は従来どおり DLL 単体でもビルドできます。
4. 配布は **`NullponSpectrum.dll` だけ**でよく、**Plugins にバンドルファイルを置かなくても**動きます。

### 開発時に Plugins で上書きしたい場合

**`Beat Saber/Plugins/nullponspectrum_shaders`** を置くと、**埋め込みより優先**して読み込みます（中身の差し替えテスト用）。

---

## 6. 動作確認

- ゲーム起動後、**`Logs`** または IPA のログで  
  `ShaderBundle: 'nullponspectrum_shaders'（埋め込みリソース）からシェーダ N 個を読み込みました`  
  または **`（Plugins ファイル）`** と出ていれば成功。
- バンドルが無い場合は  
  `ShaderBundle: 'nullponspectrum_shaders' が Plugins にも DLL 埋め込みにもありません`  
  と出るだけで、**本体は落ちず**カスタムシェーダだけ無効になります。

---

## 7. シェーダ名について

`GetShader` は **シェーダファイル先頭の** `Shader "Custom/StageProceduralSparks"` の **文字列全体**と一致する必要があります。  
バンドルに入れるのは **Shader アセット**でも **それを参照する Material** でも構いませんが、最終的に **その Shader がバンドルに含まれる**こと。

---

## 8. トラブル時

- **ピンク表示**: Unity 版がゲームと合っていない、または Built-in パイプライン用シェーダとゲームが一致していないことが多いです。
- **読み込んだのに GetShader が null**: バンドル名・ファイル名の typo、シェーダの `Shader "..."` 名の不一致を確認。
