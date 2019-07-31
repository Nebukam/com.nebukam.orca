using UnityEngine;

namespace Nebukam.ORCA
{
    public class ORCA
    {

        #region instance
        
        static private ORCA m_instance = null;
        static public ORCA instance {
            get {
                if(m_instance == null){ m_instance = new ORCA(); }
                return m_instance;
            }
        }

        [RuntimeInitializeOnLoadMethod]
        static void StaticInit()
        {
            if (m_instance == null)
            {
                m_instance = new ORCA();
            }
        }

        #endregion

        protected ORCASolverComponent m_defaultSolver = null;
        public ORCASolverComponent defaultSolver
        {
            get { return m_defaultSolver; }
            set { m_defaultSolver = value; }
        }

        public void Register(ORCASolverComponent solverComponent, bool makeDefaultSolver)
        {
            if (makeDefaultSolver)
            {
                m_defaultSolver = solverComponent;
            }
        }

        public void Unregister(ORCASolverComponent solverComponent)
        {
            if(m_defaultSolver == solverComponent)
            {
                m_defaultSolver = null;
            }
        }

    }
}
