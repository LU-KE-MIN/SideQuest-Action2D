# SideQuest-Action2D | Unity C# Architecture Study Project

![Unity](https://img.shields.io/badge/Unity-6000.2.9f1-black?logo=unity)
![C#](https://img.shields.io/badge/Language-C%23-239120?logo=c-sharp&logoColor=white)
![Architecture](https://img.shields.io/badge/Architecture-Modular%20%2F%20Asmdef-blueviolet)

## 📖 Project Overview / プロジェクト概要

**「大規模開発に耐えうる、保守性の高いUnity設計」を検証するための技術デモプロジェクトです。**

本プロジェクトは、単にゲームを作ることを目的とせず、私がバックエンドエンジニアとして培った**「疎結合（Loose Coupling）」**や**「責務の分離（Separation of Concerns）」**といった設計思想を、Unityのクライアント開発に適用する実験（Proof of Concept）として開発しています。

**⚠️ Note:** 本リポジトリはアーキテクチャ検証用であり、ゲームプレイループは開発途中です。ソースコードの構成（Assembly Definition による依存関係の整理など）を中心にご覧ください。

---

## ⚙️ Technical Highlights / 技術的なこだわり

ゲーム業界でのチーム開発を意識し、スパゲッティコードを防ぐための以下の設計を実践しています。

### 1. Assembly Definition (Asmdef) によるレイヤー分割
コンパイル時間の短縮と、循環参照の防止を目的として、機能を明確にモジュール化しています。

| Module | Namespace | Responsibility |
| :--- | :--- | :--- |
| **Game.Core** | `Core` | 共通ユーティリティ、定数、基盤システム（特定のゲームロジックに依存しない） |
| **Game.Combat** | `Combat` | ダメージ計算、Projectile（弾丸）の挙動、ヒット判定 |
| **Game.Skills** | `Skills` | スキルデータの定義、発動ロジック、インベントリ管理 |

### 2. モジュラー式スキルシステム (Scalability)
* `ScriptableObject` を活用し、データ（パラメータ）とロジック（振る舞い）を分離。
* `SkillInventory` と `SkillActivator` に責務を分けることで、将来的に新しいスキルタイプを追加しても既存コードへの影響を最小限に抑える設計です。

### 3. ステータス管理の一元化
* `PlayerStats` クラスにてレベル、経験値、攻撃力などを管理。
* イベント駆動（C# Events）により、ステータスの変化をUIや他のシステムに通知する仕組みを想定しています。

---

## 🗂 Project Structure / ディレクトリ構成

```text
Assets/
 ┣ _Project/
 ┃  ┣ Scripts/
 ┃  ┃  ┣ Core/        # 汎用基盤 (Logger, Extensions)
 ┃  ┃  ┣ Combat/      # 戦闘ロジック (Damage calc, Hitbox)
 ┃  ┃  ┗ Skills/      # スキルシステム (SO definitions)
 ┃  ┣ Scenes/         # 検証用シーン
 ┃  ┗ Art/            # アセット素材
 ┗ External/          # 外部ライブラリ

🛠 Future Roadmap / 今後の実装予定
現在はアーキテクチャの基盤構築が完了しており、今後は以下の実装を通じて「新技術のキャッチアップ」を進める予定です。

[ ] New Input Systemへの移行: Input.GetKeyDown からのアクションマッピングへの変更

[ ] Projectileシステムの拡張: オブジェクトプールを活用したメモリ管理の最適化

[ ] 敵AIの実装: ステートパターンを用いた行動ロジックの構築

🧑‍💻 Motivation / 開発の背景
「Enterprise C# Quality in Game Development」

金融・業務システムのサーバーサイド開発で求められる**「堅牢性」や「可読性」**は、複雑化する近年のゲーム開発においても不可欠です。 私は本プロジェクトを通じて、C#の強みを最大限に活かした「壊れにくく、拡張しやすい」ゲーム設計の実践を目指しています。

👤 Author
LU-KE-MIN

Backend Engineer aiming for Game Server / Client Architecture roles.

Skills: C# (.NET), SQL Server, Unity, Git.

📜 License
Personal Portfolio (Source code viewing is allowed for recruitment purposes).
