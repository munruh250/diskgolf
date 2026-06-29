# Skybox art (Disk Golf)

## Texture format

Use a **2:1 equirectangular panoramic** image (e.g. `2048×1024` or `4096×2048`).

| Region of PNG | Maps to in world |
|---------------|------------------|
| **Top edge** (row 0) | Straight up (zenith) |
| **Horizontal center line** (50% height) | Horizon ring — parallel to the ground |
| **Bottom edge** (row height) | Straight down (nadir) |

Paint your visible horizon (sky meeting distant hills/haze) on the **vertical center line of the image**.

Import settings in Unity:

- Texture Type: **Default** (not Sprite)
- sRGB: **On**
- Wrap Mode: **Clamp** on V

## Horizon in Game view vs in the PNG

These are **not** the same thing.

- **PNG 50%** = where the horizon lives on the inside of the sky sphere.
- **Screen position** = depends on **camera pitch**.

Our cameras look **down** at the fairway (editor overview ~42°, throw cam ~15–25° down). When pitched down you see more sky above and the horizon band moves **up** toward the top third — or off-screen entirely. You will almost never see the horizon across the middle of the screen during normal play.

To sanity-check your PNG:

1. Temporarily pitch the Scene camera **level** (look at the horizon, not the ground).
2. The horizon in your texture should cross the **center** of the view.
3. If it is too high/low while level, **move the horizon in the PNG** — the Panoramic shader has no vertical offset.

## Materials

| Asset | Purpose |
|-------|---------|
| `MAT_PrototypeSkybox.mat` | Default clear sky (`sky_clear`) |
| `MAT_Sky_Overcast.mat` | Grey overcast variant (`sky_overcast`) |

Both use Unity's **Skybox/Panoramic** shader (`_Mapping = 1`).

| Property | What it does |
|----------|----------------|
| `_MainTex` | Your panoramic PNG (required) |
| `_Tint` | Color multiply — use white `(1,1,1)` to see the texture faithfully; blue tint for stylized look |
| `_Exposure` | Brightness |
| `_Rotation` | Spins the panorama horizontally (degrees) — does **not** raise/lower the horizon |

Assign the texture to `_MainTex`. Leave `_Tint` at white while authoring; add color grading after the art reads correctly.

## Course editor

1. **Object → Skybox**
2. Pick a skybox icon (from the active theme's `skyboxes` list)
3. Selection is saved on the hole as `hole.skyboxId` in JSON

Runtime/editor apply: `SceneEnvironmentApplier` sets `RenderSettings.skybox` and `SceneLightingBootstrap` configures sun + camera clear flags.

## Adding a new sky

1. Drop a panoramic PNG under this folder (fix import to **Default** if Unity marks it Sprite)
2. Duplicate a `.mat`, point `_MainTex` at your texture, set `_Tint` to white, adjust `_Exposure`
3. Add a `SkyboxArchetypeEntry` to your `ThemePack` asset (`skyboxId`, `material`, optional `preview` sprite for the editor icon)
4. Run **Disk Golf → Course → Create Default Theme Pack** if you need the temperate pack repaired
