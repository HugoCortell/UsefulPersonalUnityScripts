# Animancer

[Animancer](https://kybernetik.com.au/animancer/) isn't directly compatible with Purrnet, as it bypasses the normal flow and functionality of the animator component upon which Purrnet's [Network Animator](https://purrnet.gitbook.io/docs/plug-n-play-components/network-animator) component relies on to keep animations syncronized across clients.

However, it is both fast and easy use [RPCs](https://purrnet.gitbook.io/docs/systems-and-modules/remote-procedure-call-rpc) to re-create the functionality of the network animator in a way that it is compatible with animancer.

## Using RPCs To Keep Animancer Synced Across Clients

For this example, we're using [Unsafe Network Rules](https://purrnet.gitbook.io/docs/terminology/client-auth-everyone-unsafe), to keep things simple and focused. You may use a safer and more complex setup at your convenience.

First, create your character and set it up as you would a normal character with animancer, meaning that you're adding the following components:
* Mesh, colliders, controllers, etc
* Animator, with an empty controller assigned to it (this will stop errors from printing in the console).
* Your player/character controller script.

Next, if you haven't already, use Purrnet to network your player controller in whatever way you want, [here is an example you can follow](https://purrnet.gitbook.io/docs/full-game-guides/first-person-shooter). But, **don't manage any animations directly inside of your player controller script,** make sure to also disable your controller script if it isn't owned by you `enabled = isOwner;`.

```csharp
protected override void OnSpawned()
{
	base.OnSpawned();

	enabled = isOwner;

	if (!isOwner) { Destroy(PlayerCameraObject); }
}
```

With that done, you're now going to want to do two things to prepare your player controller.

For starters: you'll want to start tracking what animation your player should be playing using a variable, without actually playing the animation itself. You may do this however you want, but I recommend using an enumerator.

```csharp
public enum PlayerMovementState // Create one enumerator for each animation you have
{
	Idle,
	Moving,
	Running,
	Crouching,
	Sneaking,
	Jumping,
}
```

```csharp
// This code is just for illustrative purposes, make your own logic for storing the animation state (the animation your character should play) based on your needs.
PlayerMovementState CurrentState = LastMovementState;
IsMoving		= MovementInputValues.magnitude > 0f;
CurrentState	= IsMoving ? PlayerMovementState.Moving : PlayerMovementState.Idle;

if (!IsGrounded || JumpPressed) { CurrentState = PlayerMovementState.Falling; }
else
{
	if (IsMoving)
	{
		if (CrouchPressed)	{ CurrentState = PlayerMovementState.Sneaking; }
	}
	else
	{
		if (CrouchPressed)	{ CurrentState = PlayerMovementState.Crouching; }
	}
}
```

Now, each client is locally storing a variable representing which animation they should be playing, but this isn't actually playing an animation, nor is it sending any data over the network. To do that, we're going to create a second script and place it next to our player controller script, this new script will manage and sync our animations over the network. Unlike our player controller, this one won't be disabled based on ownership.

Here's is a complete example of that script:

```csharp
using System.Collections.Generic;
using Animancer;
using PurrNet;
using UnityEngine;

public class OurNetworkedAnimator : NetworkBehaviour
{
	[Header("References")]
	public AnimancerComponent AnimancerReference;
	
	[Header("Animation Clips")]
	private Dictionary<PlayerMovementState, AnimationClip> AnimationLookup;
	public AnimationClip Animation_Idle;
	public AnimationClip Animation_Walk;
	public AnimationClip Animation_Run;
	public AnimationClip Animation_Crouch_Idle;
	public AnimationClip Animation_Crouch;
	public AnimationClip Animation_Jump;

	void Awake()
	{
		// We can use a dictionary to simplify looking up an AnimationClip based on our PlayerMovementState enumerator
		AnimationLookup = new Dictionary<PlayerMovementState, AnimationClip>
		{
            { PlayerMovementState.Idle, 		Animation_Idle },
            { PlayerMovementState.Falling, 		Animation_Jump },
			{ PlayerMovementState.Moving,		Animation_Walk },
			{ PlayerMovementState.Running,		Animation_Run },
			{ PlayerMovementState.Jumping,		Animation_Jump },
			{ PlayerMovementState.Crouching,	Animation_Crouch_Idle },
			{ PlayerMovementState.Sneaking,		Animation_Crouch }
        };
	}
	
	[ObserversRpc] // Observer RPC means that we call this code to execute on all clients, including the sender.
    public void PlayAnimationSynced(PlayerMovementState MovementState)
    {
		AnimancerReference.Play(AnimationLookup.GetValueOrDefault(MovementState, Animation_Idle)); // We use GetValueOrDefault to play Animation_Idle in the event that our look-up fails
	}
}
```

In this script, we have a dictionary that stores all of our `AnimationClip` variables and links each one of them to a `PlayerMovementState` enumerator value.

Then, we have a `[ObserversRpc]` function which is `public`, this function recieves our `PlayerMovementState` and executes `AnimancerReference.Play` for the owner of that script on every client.

Finally, In your player controller, assuming that your logic to decide the `PlayerMovementState` is in `Update`, you're going to want to add this at the end:

```csharp
// Our player controller has a reference to OurNetworkedAnimator and uses it to call PlayAnimationSynced
OurNetworkedAnimatorReference.PlayAnimationSynced(CurrentState); // Passing the CurrentState variable of type PlayerMovementState
```

If a player calls `PlayAnimationSynced` from their player controller (through a reference to `OurNetworkedAnimator`), the animation clip played by `AnimancerReference.Play` will then play in sync across all clients, working in the same way as Network Animator does. Just like that, you've successfully synced animancer animations over the network. *Congratulations!*

## Eliminating Unecessary RPCs

Currently, you are sending an RPC over the network every frame. Preferably, we'd want to only send out data to other clients when a change has occured that we want to keep in sync. If you recall, in our earlier example for setting the `PlayerMovementState` enum, we're creating a new `PlayerMovementState` with an assignment of `LastMovementState` each `Update` cycle (`PlayerMovementState CurrentState = LastMovementState;`), this was precisely to avoid unecessary RPC calls.

In your player controller script that sets the `PlayerMovementState`, you want to store a `LastMovementState` variable inside your class: `private PlayerMovementState LastMovementState = PlayerMovementState.Idle;`. This will help us keep track of what the resulting `PlayerMovementState` was at the end of the last `Update` cycle. As before, make sure your logic defines a new `PlayerMovementState` at the start of each update cycle (`PlayerMovementState CurrentState = LastMovementState;`), then *only write to that new variable* as your logic to find the correct `PlayerMovementState` for that frame executes.

Next, you'll want to replace `OurNetworkedAnimatorReference.PlayAnimationSynced(CurrentState);` with:
```csharp
if (CurrentState != LastMovementState) // Avoid making an RPC when no changes have happened between frames.
{
	// Assign LastMovementState to reflect the change
	LastMovementState = CurrentState;

	// Then call the RPC
	OurNetworkedAnimatorReference.PlayAnimationSynced(LastMovementState);
}
```

*Congratulations.* You network isn't saturated by excess data any more, and your logic to keep client animations in sync is much closer to how the actual Network Animator component works.

## Making Full Use of Animancer's Power

Animancer has many variables and settings that can be defined to adjust the transitions between animations, as well as other things. Currently, to keep our example guide simple and straightforward, all we're doing is directly play a clip based on the `PlayerMovementState` that our RPC function recieves. To unlock the full potential of animancer, all you need to do is either pass more arguments to `PlayAnimationSynced` which can be used to define parameters in animancer, or use structs to assemble more complex versions of `PlayerMovementState` that include extra information for animancer to use.
