// Assets/_Project/Scripts/Skills/Editor/MdCodeSplitter.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Game.Skills.EditorTools
{
    public static class MdCodeSplitter
    {
        // 預設輸出根目錄（會自動 fallback）
        static readonly string DefaultRootA = "Assets/_Project/Scripts";
        static readonly string DefaultRootB = "Assets/Scripts";

        [MenuItem("Tools/Skills/Import Code From Markdown...")]
        public static void ImportFromMd()
        {
            // 1) 選 md 檔
            string mdPath = EditorUtility.OpenFilePanel("Select Markdown File", Application.dataPath, "md");
            if (string.IsNullOrEmpty(mdPath)) return;

            // 2) 決定輸出根（優先 _Project/Scripts，其次 Assets/Scripts，再不行讓你選）
            string root = AssetDatabase.IsValidFolder(DefaultRootA) ? DefaultRootA :
                          AssetDatabase.IsValidFolder(DefaultRootB) ? DefaultRootB :
                          AssetDatabase.GenerateUniqueAssetPath("Assets/Scripts");
            if (!AssetDatabase.IsValidFolder(root))
            {
                string custom = EditorUtility.OpenFolderPanel("Select Output Root (Scripts)", Application.dataPath, "");
                if (string.IsNullOrEmpty(custom)) return;
                if (!custom.StartsWith(Application.dataPath))
                {
                    EditorUtility.DisplayDialog("Error", "Output folder must be inside Assets/", "OK");
                    return;
                }
                root = "Assets" + custom.Substring(Application.dataPath.Length);
            }

            // 3) 讀 md
            string md = File.ReadAllText(mdPath, new UTF8Encoding(false));

            // 4) 解析：找 "### 檔名.cs" + 後面的 ```csharp ... ```
            //   - 標題：行開頭 ### 或 ## 皆可；允許後綴文字
            //   - 只抓第一個 ```csharp 到下一個 ``` 的內容
            var pattern = new Regex(@"^[#]{2,3}\s+([A-Za-z0-9_\.]+\.cs).*?\r?\n```csharp\r?\n([\s\S]*?)\r?\n```",
                                    RegexOptions.Multiline);

            var matches = pattern.Matches(md);
            if (matches.Count == 0)
            {
                EditorUtility.DisplayDialog("No Code Found",
                    "未在 Markdown 中找到「### 檔名.cs」＋「```csharp」的區塊。\n請確認你的檔案標題與程式碼柵欄格式。", "OK");
                return;
            }

            // 5) 建立輸出：依檔名推斷子資料夾
            int ok = 0;
            var summary = new StringBuilder();
            foreach (Match m in matches)
            {
                string fileName = m.Groups[1].Value.Trim();
                string code = m.Groups[2].Value;

                string sub = GuessSubfolder(fileName, root);
                string dir = Path.Combine(root, sub).Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    // 逐層建立
                    EnsureFolders(dir);
                }

                string assetPath = Path.Combine(dir, fileName).Replace("\\", "/");
                string projectAssets = Application.dataPath; // .../YourProject/Assets
                string relativeFromAssets = assetPath.Substring("Assets".Length); // "/_Project/..."
                string fullPath = System.IO.Path.Combine(projectAssets, relativeFromAssets.TrimStart('/'))
                                             .Replace("/", System.IO.Path.DirectorySeparatorChar.ToString());
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));

                System.IO.File.WriteAllText(fullPath, code, new System.Text.UTF8Encoding(false));
                summary.AppendLine(assetPath);
                ok++;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done",
                $"已匯入 {ok} 個檔案：\n\n{summary}", "OK");
        }

        // 依檔名猜測子資料夾。若該資料夾不存在，回退到合理的既有資料夾。
        static string GuessSubfolder(string fileName, string root)
        {
            string lower = fileName.ToLower();

            // 優先依關鍵字分類
            if (lower.Contains("definition")) return Prefer(root, "Skills/Definitions");
            if (lower.Contains("effect")) return Prefer(root, "Skills/Effects");
            if (lower.Contains("executor")) return Prefer(root, "Skills/Executors");
            if (lower.Contains("inventory")) return Prefer(root, "Skills/Inventory", "Skills/Runtime");
            if (lower.Contains("activator")) return Prefer(root, "Skills/Activation", "Skills/Runtime");
            if (lower.Contains("inputhandler")) return Prefer(root, "Skills/Activation", "Skills/Runtime");
            if (lower.Contains("passive")) return Prefer(root, "Skills/Passive", "Skills/Runtime");
            if (lower.Contains("instance")) return Prefer(root, "Skills/Runtime");
            if (lower.Contains("parameter")) return Prefer(root, "Core/Data", "Core");
            if (lower.Contains("statuseffect")) return Prefer(root, "Core/Data", "Core");
            if (lower.Contains("events")) return Prefer(root, "Core/Events", "Core");
            if (lower.Contains("playerstats")) return Prefer(root, "Core");
            if (lower.Contains("damageable")) return Prefer(root, "Combat");
            if (lower.Contains("projectile")) return Prefer(root, "Combat");
            if (lower.Contains("ui") || lower.EndsWith("view.cs"))
                return Prefer(root, "UI");

            // 介面：I 開頭常放 Interfaces
            if (Path.GetFileNameWithoutExtension(fileName).StartsWith("I"))
                return Prefer(root, "Core/Interfaces", "Core");

            // 預設回 Skills/Runtime
            return Prefer(root, "Skills/Runtime");
        }

        // 在 root 下挑第一個存在的路徑；都不存在就回傳第一個（稍後由 EnsureFolders 建）
        static string Prefer(string root, params string[] subpaths)
        {
            foreach (var s in subpaths)
            {
                string p = Path.Combine(root, s).Replace("\\", "/");
                if (AssetDatabase.IsValidFolder(p)) return s;
            }
            return subpaths[0];
        }

        // 逐層建立資料夾（支援 "A/B/C"）
        static void EnsureFolders(string assetPath)
        {
            // assetPath: Assets/.../Sub/Sub2
            var parts = assetPath.Split('/');
            string build = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = build + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(build, parts[i]);
                }
                build = next;
            }
        }
    }
}
#endif
