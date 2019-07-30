using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebukam.ORCA
{
    
    public class ORCAAgentGroup : ORCAAgent
    {

        protected List<IORCAAgent> m_childs = new List<IORCAAgent>();
        protected Dictionary<IORCAAgent, AgentConfig> m_initialConfigs = new Dictionary<IORCAAgent, AgentConfig>();
        
        public ORCAAgentGroup()
        {
            m_requirePreparation = true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="agent"></param>
        public void Add(IORCAAgent agent)
        {
            if (m_initialConfigs.ContainsKey(agent)) { return; }

            m_initialConfigs[agent] = AgentConfig.From(agent);
            m_childs.Add(agent);
        }

        public void Remove(IORCAAgent agent)
        {
            if (!m_initialConfigs.ContainsKey(agent)) { return; }

            m_initialConfigs.Remove(agent);
            m_childs.Remove(agent);
        }

        protected void RestoreChildConfig(IORCAAgent agent)
        {

        }

        internal override void Prepare()
        {
            base.Prepare();
            //Forward group's prefVelocity to its child to ensure
            //correct reaction from other agents

        }

        internal override void Commit()
        {
            base.Commit();
        }

    }
}
