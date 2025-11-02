using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game01.GameObjects
{
    public class Campaign
    {
        // Campaign can be thought of as a bunch of branching Missions.

        Faction[] mFactions;
        object[] mObjectives;


        public Campaign(string name)
        {
            //Generate();
        }

        private void Generate(int seed)
        {
            // when vehicles are spawned, assign crew, names, rank, and station if applicable

            // generate crew
        }

        // todo: our test_ai.kgbentity should contains more crew stations
        // todo: actually, i dont think we want to generate in Campaign... i dont want Game01.dll dependant on Keystone.Dll.  So only if we generate custom properties
        //       without actually referencing the Entities
        private Character NextChararter()
        {
            Character result = new Character();

            // make sure all name s are unique

            // based on the configuration loaded, generate crew by rank, station, commanding officer, security clearance, etc
            // assign crew quarters and bunks

            // command

            // bridge crew

            // engineers & technicians & maintenance (including weapons technicians)

            // medical

            // security

            // sciences

            // communications (only need a few, maybe #shifts * 3)

            // political advisor

            // marines

            // cooks

            // Then fill out all their attributes(advantages and disadvantages) and skills and proficiencies

            return result;
        }

        private void SetAdvantagesDisadvantgates(Character c)
        {

        }

        private void SetProficiencies(Character c)
        {

        }

        void Load()
        {
        }

        void Tick()
        {
            // check if objectives have been achieved


            // one final objective could be return home with 80% of your crew and passengers\refugees\colonists alive

            // hidden objectives can be "return live alien specimen back to starbase 01

        }
    }
}
