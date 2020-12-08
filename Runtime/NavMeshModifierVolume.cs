using System.Collections.Generic;

namespace UnityEngine.AI
{
    /// <summary> Component used by the NavMesh building process to assign a different area type to the region inside the specified volume.</summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Navigation/NavMeshModifierVolume", 31)]
    [HelpURL("https://github.com/Unity-Technologies/NavMeshComponents#documentation-draft")]
    public class NavMeshModifierVolume : MonoBehaviour
    {
        [SerializeField]
        Vector3 m_Size = new Vector3(4.0f, 3.0f, 4.0f);
        /// <summary> Gets or sets the dimensions of the cuboid modifier volume. </summary>
        /// <remarks> The dimensions apply in the local space of the GameObject. </remarks>
        public Vector3 size { get { return m_Size; } set { m_Size = value; } }

        [SerializeField]
        Vector3 m_Center = new Vector3(0, 1.0f, 0);
        /// <summary> Gets or sets the center position of the modifier volume. </summary>
        /// <remarks> The position is relative to the GameObject transform. </remarks>
        public Vector3 center { get { return m_Center; } set { m_Center = value; } }

        [SerializeField]
        int m_Area;
        /// <summary> Gets or sets the area type that will be enforced by the volume during the generation of the NavMesh. </summary>
        /// <remarks> The range of useful values is from 0 to 31. Higher values always take precedence over lower values in the case when more volumes intersect each other. A value of 1 has the highest priority over all the other types and it means "not walkable". Consequently, a volume with an <c>area</c> of 1 produces a hole in the NavMesh. This property has the same meaning as <see cref="NavMeshBuildSource.area"/>.</remarks>
        /// <seealso href="https://docs.unity3d.com/Manual/nav-AreasAndCosts.html"/>
        public int area { get { return m_Area; } set { m_Area = value; } }

        // List of agent types the modifier is applied for.
        // Special values: empty == None, m_AffectedAgents[0] =-1 == All.
        [SerializeField]
        List<int> m_AffectedAgents = new List<int>(new int[] { -1 });    // Default value is All

        static readonly List<NavMeshModifierVolume> s_NavMeshModifiers = new List<NavMeshModifierVolume>();

        /// <summary> Gets the list of all the <see cref="NavMeshModifierVolume"/> components that are currently active in the scene. </summary>
        public static List<NavMeshModifierVolume> activeModifiers
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

        /// <summary> Verifies whether this modifier volume can affect in any way the generation of a NavMesh for a given agent type. </summary>
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
