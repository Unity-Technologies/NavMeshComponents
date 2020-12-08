using System.Collections.Generic;

namespace UnityEngine.AI
{
    /// <summary> Component that modifies the properties of the GameObjects used for building a NavMesh. </summary>
    /// <remarks>The properties apply to the current GameObject and recursively to all its children
    /// in the hierarchy. This modifier overrides the properties set to this GameObject by
    /// any other NavMeshModifier in the parent hierarchy.</remarks>
    [ExecuteInEditMode]
    [AddComponentMenu("Navigation/NavMeshModifier", 32)]
    [HelpURL("https://github.com/Unity-Technologies/NavMeshComponents#documentation-draft")]
    public class NavMeshModifier : MonoBehaviour
    {
        [SerializeField]
        bool m_OverrideArea;
        /// <summary> Gets or sets whether to assign the <see cref="area"/> type to this object instead of the <see cref="NavMeshSurface.defaultArea"/>. </summary>
        /// <remarks> The area type information is used when baking the NavMesh. </remarks>
        /// <seealso href="https://docs.unity3d.com/Manual/nav-AreasAndCosts.html"/>
        public bool overrideArea { get { return m_OverrideArea; } set { m_OverrideArea = value; } }

        [SerializeField]
        int m_Area;
        /// <summary> Gets or sets the area type applied by this GameObject. </summary>
        /// <remarks> The range of useful values is from 0 to 31. Higher values always take precedence over lower values in the case when the surfaces of more GameObjects intersect each other to produce a NavMesh in the same region. A value of 1 has the highest priority over all the other types and it means "not walkable". Consequently, the surface of a GameObject with an <c>area</c> of 1 produces a hole in the NavMesh. This property has the same meaning as <see cref="NavMeshBuildSource.area"/>.</remarks>
        /// <seealso href="https://docs.unity3d.com/Manual/nav-AreasAndCosts.html"/>
        public int area { get { return m_Area; } set { m_Area = value; } }

        [SerializeField]
        bool m_IgnoreFromBuild;
        /// <summary> Gets or sets whether the NavMesh building process ignores this GameObject and its children. </summary>
        public bool ignoreFromBuild { get { return m_IgnoreFromBuild; } set { m_IgnoreFromBuild = value; } }

        // List of agent types the modifier is applied for.
        // Special values: empty == None, m_AffectedAgents[0] =-1 == All.
        [SerializeField]
        List<int> m_AffectedAgents = new List<int>(new int[] { -1 });    // Default value is All

        static readonly List<NavMeshModifier> s_NavMeshModifiers = new List<NavMeshModifier>();

        /// <summary> Gets the list of all the <see cref="NavMeshModifier"/> components that are currently active in the scene. </summary>
        public static List<NavMeshModifier> activeModifiers
        {
            get { return s_NavMeshModifiers; }
        }

        void OnEnable()
        {
            if (!s_NavMeshModifiers.Contains(this))
                s_NavMeshModifiers.Add(this);
        }

        void OnDisable()
        {
            s_NavMeshModifiers.Remove(this);
        }

        /// <summary> Verifies whether this modifier can affect in any way the generation of a NavMesh for a given agent type. </summary>
        /// <param name="agentTypeID"> The identifier of an agent type that originates from <see cref="NavMeshBuildSettings.agentTypeID"/>. </param>
        /// <returns> <c>true</c> if this component can affect the NavMesh built for the given agent type; otherwise <c>false</c>. </returns>
        public bool AffectsAgentType(int agentTypeID)
        {
            if (m_AffectedAgents.Count == 0)
                return false;
            if (m_AffectedAgents[0] == -1)
                return true;
            return m_AffectedAgents.IndexOf(agentTypeID) != -1;
        }
    }
}
