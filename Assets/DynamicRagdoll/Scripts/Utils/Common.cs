
namespace DynamicRagdoll {
    public enum UpdateMode { 
        Update, 
        FixedUpdate, 
        LateUpdate 
    };

    public enum RagdollControllerState { 
        Animated, 					//fully animated
        CalculateAnimationVelocity,	//calculating while still showing fully animated
        Falling,					//decaying fall
        Ragdolled, 					//complete ragdoll
        TeleportMasterToRagdoll, 	//waiting for get up animation transition, to reorient invisible master
        BlendToAnimated, 			//blend into animated position
    };
}
