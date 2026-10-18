# CS464 Assignment 02 · Abdul Samad Sohail · BSSE23083

This is my player controller for Assignment 02. It moves relative to the camera, jumps to a set height in metres, forgives jump presses that are a little early or a little late, falls faster than it rises, squashes when it lands, and has a camera that follows smoothly at any frame rate. I did it in three levels: Level 1, Level 3 and Level 4.

- **Video:** https://drive.google.com/file/d/1dM7JelzRUf6HkK3sYr6XORPDaDEUvwYb/view?usp=sharing
- **Input:** Legacy Input with Active Input Handling set to Both. Movement uses `Input.GetAxis("Horizontal")` and `("Vertical")`, jumping uses `Input.GetKeyDown(KeyCode.Space)`, and all of it is read in `Update`.
- **Ground check:** `Physics.CheckSphere` at the player's feet, with only the Ground layer in the mask. The player isn't on that layer, so it can't detect itself. With the player selected there's a wire sphere gizmo that is green on the ground and red in the air.
- **Camera:** exact smoothing with `1 - e^(-k·dt)` in `LateUpdate`, plus look-ahead from the player's velocity.
- **Juice:** a squash on landing using `Ease.OutBack`, run by a coroutine. It starts when the player lands after more than 0.1 s in the air.

## How it's put together

The player is a prefab of a person holding a gun. The root is a GameObject with a Rigidbody, a capsule collider (height 2, radius 0.5) and `PlayerController`. The character model sits on a child called `Body` with no collider, so squashing it doesn't change the hitbox. Rotation is frozen, interpolation is on, and the collider has a zero-friction material (combine set to Minimum) so the player doesn't stick to walls in mid-air.

`Update` reads the keys, builds the movement direction from the camera, checks the ground, runs the coyote and buffer timers, and starts the squash. `FixedUpdate` sets the velocity, applies a queued jump once, and adds extra gravity while falling.

For camera-relative movement I flatten the camera's forward and right, normalize them, and combine them with the input. I tested it by setting the camera offset to (7, 4, 0) in Play mode, and W still walked away from the camera.

The icons in the levels are prefabs too, but I removed their colliders so they can't be touched or collided with.

## Tuning

| Setting | Value | Why this number |
|---|---|---|
| Speed | 5 m/s | Same as the Lecture 04 template. It crosses a level quickly but is still easy to control, and a jump at this speed carries about 5 m. |
| Jump height | 1.8 m | Clears a 1.2 m ledge with 0.6 m to spare, but it can't jump a 2 m wall. |
| Time to apex | 0.61 s | It follows from the jump height: v = 5.94 m/s, so t = v / g = 0.61 s. Short enough that the jump feels snappy. |
| Fall multiplier | 2.2× | The apex stays at 1.8 m but the fall takes 0.41 s instead of 0.61 s, so the jump feels weighty instead of floaty. |
| Coyote time | 0.1 s | At 5 m/s that's 0.5 m past the edge. Enough to forgive a late press, not enough to feel like jumping off thin air. |
| Jump buffer | 0.12 s | Falling from the apex I land at about 8.8 m/s, so 0.12 s before landing is roughly 0.9 m up. An early press from there still counts. |
| Camera smoothing | sharpness 5 | Time constant is 1 / 5 = 0.2 s, so at 5 m/s the camera trails about 1 m (speed ÷ k). Smooth, but the player stays in view. |
| Look-ahead | 0.35 s | At 5 m/s the aim point leads by 1.75 m. After the 1 m trail the camera ends up about 0.75 m ahead of the player. |

## Four answers

1. **The jump, in numbers.** With `jumpHeight` 1.8 and g = 9.81, the launch speed is v = √(2 · 9.81 · 1.8) = 5.94 m/s. The mass is 1, so `jumpForce` (the impulse) is m · v = 5.94. Time to the apex is 5.94 / 9.81 = 0.61 s. The fall back down at 2.2× gravity takes √(2 · 1.8 / (9.81 · 2.2)) = 0.41 s. So a jump lasts about 1.01 s and carries me about 5.1 m at 5 m/s. Because physics steps are 0.02 s, the real peak comes out a bit lower, around 1.74 m. My levels don't have a gap the player has to jump across, so there's no gap to clear, but if there were one, anything under about 5 m would be fine.

2. **Update and FixedUpdate.** `Update` runs every frame, so that's where I read the keys, work out the camera-relative direction, check the ground, run the coyote and buffer timestamps, queue the jump and start the squash. `FixedUpdate` runs every 0.02 s, so that's where I set `rb.linearVelocity` (keeping y), apply the queued jump once, clear the flag, and add the fall gravity. If I swapped them, reading `GetKeyDown` in `FixedUpdate` would drop presses, since at 144 fps most frames have no physics step and `GetKeyDown` is only true for one frame. And doing the velocity and forces in `Update` would make the jump and fall gravity depend on the frame rate.

3. **Vector3.Dot.** My ground check uses a `CheckSphere`, so I don't use Dot for that. One place it would fit is `Vector3.Dot(move, forward)` in `PlayerController.Update`, where `forward` is the camera's flattened forward. Positive means the player is walking away from the camera, negative means towards it, and around zero means sideways. It would also work for a contact-normal ground check: `Vector3.Dot(normal, Vector3.up) > 0.5f` accepts floors and rejects walls.

4. **The juice.** It's a squash on landing. `Body` starts at 70% height (with x and z at about 1.2 to keep the volume) and goes back to normal along `Ease.OutBack`, which overshoots a little and settles. It lasts 0.25 s, which is 15 frames at 60 fps. I picked that so it's long enough to notice but short enough not to get in the way of the next jump. I used `LerpUnclamped` so the overshoot doesn't get cut off. It only plays after more than 0.1 s in the air, so small steps don't trigger it, and if one is already playing it gets stopped first.

## Levels

| Level | What the controller must do there | What I changed for this jump |
|---|---|---|
| Level01 | Move through a fighting gun game level, past obstacles and checkpoints, to the goal. No jump is needed to get anywhere, but the jump is there. | I moved some obstacles and barriers because the player couldn't get past them. |
| Level03 | Same kind of level: get past the obstacles and through the checkpoints. No jump is needed to progress. | I added box colliders and moved a few obstacles so the player can pass without getting caught on anything. |
| Level04 | Same kind of level again: get past the obstacles and through the checkpoints. No jump is needed to progress. | Only the tree colliders and the taller obstacle colliders described below. |

All three levels have trees. The player was climbing up them, so I gave each tree a box collider. A few obstacles had the same problem, so they got a box collider with more height. The icons placed in the levels had their colliders removed so the player can't touch them.
