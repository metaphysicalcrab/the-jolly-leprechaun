# Leprechaun Character Animations

## Overview
- **Total Animations**: 18
- **Rig Type**: Humanoid (22 DEF_ bones)
- **Frame Rate**: 30 fps
- **Export Format**: glTF 2.0 (.glb)

## Animation List

### idle
- **Frames**: 1 - 60 (60 frames)
- **Loop**: Yes
- **Description**: Relaxed breathing idle with subtle body movement
- **Notes**: Default state, use as base for animation blending

### walk
- **Frames**: 1 - 32 (32 frames)
- **Loop**: Yes
- **Description**: Casual walking cycle
- **Notes**: Standard locomotion speed

### run
- **Frames**: 1 - 16 (16 frames)
- **Loop**: Yes
- **Description**: Jogging/running cycle
- **Notes**: Faster cycle than walk, good for normal movement speed

### sprint
- **Frames**: 1 - 12 (12 frames)
- **Loop**: Yes
- **Description**: Fast sprinting cycle
- **Notes**: Shortest cycle, use for maximum movement speed

### jump
- **Frames**: 1 - 30 (30 frames)
- **Loop**: No
- **Description**: Jump up and land
- **Notes**: Single-shot animation, blend back to idle/walk on landing

### skip
- **Frames**: 1 - 23 (23 frames)
- **Loop**: Yes
- **Description**: Playful skipping movement
- **Notes**: Can be used for happy/celebration movement

### sneak_idle
- **Frames**: 1 - 59 (59 frames)
- **Loop**: Yes
- **Description**: Crouched sneaking idle with subtle breathing, weight shifts, and nervous head turns
- **Notes**: Establishes sneak base pose (hips lowered, forward lean, knees bent). Use as default state for all sneak behaviors

### sneak_walk
- **Frames**: 1 - 39 (39 frames)
- **Loop**: Yes
- **Description**: Standard sneaking walk cycle maintaining crouched posture
- **Notes**: Smaller steps than normal walk, subtle arm counter-swing close to body

### sneak_tiptoe
- **Frames**: 1 - 47 (47 frames)
- **Loop**: Yes
- **Description**: Exaggerated cartoon tiptoeing with high knee lifts and nervous head swiveling
- **Notes**: Classic "Scooby-Doo" style sneak. On tiptoes, arms wide for balance, very slow movement

### sneak_run
- **Frames**: 1 - 23 (23 frames)
- **Loop**: Yes
- **Description**: Fast crouched running cycle with aggressive arm pump
- **Notes**: Same crouch as sneak_idle but faster tempo and larger amplitude. Head stays forward-focused

### victory
- **Frames**: 1 - 59 (59 frames)
- **Loop**: Yes
- **Description**: Fist-pump celebration with hip shimmy and bouncy energy
- **Notes**: In-place celebration. Right fist pumps overhead, left hand on hip

### victory_jump
- **Frames**: 1 - 50 (50 frames)
- **Loop**: No
- **Description**: Excited jump with heel-click at apex, heavy landing into deep squat
- **Notes**: Transitions well into victory loop at end. Shows portly character weight on landing

### hurt
- **Frames**: 1 - 30 (30 frames)
- **Loop**: No
- **Description**: Hit reaction with backward recoil and recovery
- **Notes**: Frame 1 IS the impact (no wind-up). Slight backward stagger. Returns to near-idle for blending

### loser_fallover
- **Frames**: 1 - 60 (60 frames)
- **Loop**: No
- **Description**: Dramatic comedic backward fall - despair to timber to lying flat
- **Notes**: "Woe is me" hand-to-forehead, then stiff-as-a-board backward fall. Ends lying on back

### double_jump
- **Frames**: 1 - 30 (30 frames)
- **Loop**: No
- **Description**: Mid-air tuck and 360-degree spin from jump apex, extending to land
- **Notes**: Chain after jump animation. Full Z-axis rotation with body tuck. Arms pull in for spin, extend for landing

### scare
- **Frames**: 1 - 40 (40 frames)
- **Loop**: No
- **Description**: "Magical girl" spin with 360-degree rotation into dramatic scary pose reveal
- **Notes**: Wind-up crouch, spin with arms flung out (implied clothes fly off), lands in wide power stance with arms raised. **Godot**: Add property tracks to hide CHAR_shirt and PROP_hat at frame 10 (~0.33s)

### sneak_stumble_roll
- **Frames**: 1 - 45 (45 frames)
- **Loop**: No
- **Description**: Sneaking trip into stumble, forward roll, and recovery back to sneak crouch
- **Notes**: Starts and ends in sneak base pose for seamless blending. Roll rotation distributed across spine bones. Root motion moves character forward ~0.65m

### initiate_transform
- **Frames**: 1 - 50 (50 frames)
- **Loop**: No
- **Description**: Magical girl transformation start - surprised gasp, energy coil, 720-degree floating spin into dramatic power pose
- **Notes**: "Safe for work" transformation beginning. Portly leprechaun doing graceful balletic spin is intentionally comedic. Two full rotations with parabolic rise arc

## Godot Import Notes
- Import as AnimationLibrary for AnimationPlayer/AnimationTree
- All animations use root bone (DEF_hips) for root motion
- Bone influences limited to 4 per vertex for compatibility
- Recommended blend time between animations: 0.2s
- Sneak animations share a common base pose - blend between them freely
- victory_jump transitions naturally into victory loop
- hurt returns to near-idle pose for clean blending
- loser_fallover ends in lying position - needs separate get-up animation or respawn
- sneak_stumble_roll has forward root motion (~0.65m) - account for position offset
