using System;
using System.Collections.Generic;


namespace Game01
{
    public class ProcGen
    {

        // todo: I think KeyCommon.DatabaseEntities is wrong...  that should be left to the game01.dll and the client.exe and server.exe.
        //       And looking at the code, there is NONE!  Even here we use Database.AppDatabaseHelper to create and modify the database.
        //       But before i delete the DatabaseEntities folder, save some of the useful thoughts, comments and links.
        // todo: Even Vehicle record does not use KeyCommon.DatabaseEntities.  
        //       So i do think Database functionality should be part of the exe's and not even directly available to the scripts except through a PersistAPI or StorageAPI
        //       But also keeping in mind that most database data once loaded, remains in memory and only gets saved periodically and/or when closing the arcade.
        // todo: Game01.GameObjects.Character.Initialize (crewCount);
        // todo: how do we serialize/deserialize character data?  Originally it used netbufer read/write but ive gotten rid of that under KeyCommon.DatabaseEntities.Character and its now Game01.GameObjects.Character
        //          Game01.GameObjects.Character characterData = bonedEntities[0].CustomData.GetObject("character") as Game01.GameObjects.Character;
        //  
        public static GameObjects.Character[] CreateCharacters(int count, int seed)
        {
            if (count <= 0) return null;

            GameObjects.Character[] results = new GameObjects.Character[count];

            // https://stackoverflow.com/questions/19270507/correct-way-to-use-random-in-multithread-application
            //https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-write-a-simple-parallel-for-loop
            System.Threading.Tasks.Parallel.For(0, results.Length, i =>
            {
                CreateCharacter(seed + i, i, ref results);
            });
              

            return results;
        }

        private static void CreateCharacter(int seed, int index, ref GameObjects.Character[] characters)
        {
            // http://brentnewhall.com/games/gurps_4e.php
            // https://devilghost.com/software/travellercharacter/
            // https://imaginware.tripod.com/imaginware/IMWStarshipBuilder.html
            // todo: the above shows just how complex a single NPC can be.
            // todo: we should just start off simply and then wait for v2.0 to be more complex
            // also maybe limit the advantages/disadvantages and skills/proficiences to just the ones in Space, First In, and Traveller

            characters[index] = new GameObjects.Character();
            AssignCharacterInfo(characters[index], seed); // todo: what if the generated Biography and History resembled a wiki page? All knowledge base pages could be that way. research wiki generation
            AssignDuties(characters[index]); // todo: this needs lots of work.  we need more crew for some things than others so it cant be completely random.
            AssignCrewQuarters(characters[index]); // todo: each bunk needs a unique friendly name? Do we need to assign the door friendly name too and then all "areas" on the other side of that door?
        }

        private static void AssignCharacterInfo(GameObjects.Character character, int seed)
        {
            Random random = new Random(seed);

            // todo: currentTask is that customproperty or script data object?
            // todo: we could even store an array of tasks as a propertyspec in the script
            List<string> samples = new List<string>() { "Joseph", "Serena", "Robert", "Alicia", "Sam", "Veronica", "Barack", "Michelle", "Brian", "Katherin", "Londessa", "Edith", "Ursula", "Preston", "Adriana", "Maria" };
            Keystone.Utilities.MarkovNameGenerator nameGen = new Keystone.Utilities.MarkovNameGenerator(samples, 1, 2, seed);

            

            // nationality

            // name
            character.FirstName = nameGen.NextName;
            character.MiddleName = nameGen.NextName;
            character.LastName = nameGen.NextName;
            // species

            // gender
            character.Gender = random.Next(2);
            // age

            // rank (Admiral, Vice Admiral, Rear Admiral, Commodore, Captain, Commander, Lieutenant Commander, Lieutenant, Ensign, Master Chief Petty Officer, Senior Chief Petty Officer, Chief Petty Officer, Petty Officer, 1st Class, Petty Officier, 2nd Class, Crewman 3rd Class, Crewman 2nd Class, Crewman 1st Class, Cadet)

            // profession/duties

            // awards/accodmodations

            // demotions/"On Report" notices (reason why included)

            // place of birth

            // professional history

            // background information


            // serializable = false; // do not save them to the vehicle.kgbentity, just save them as independant files in the //saves// path

            // skills & proficiencies

            // traits

            // basic stats (Int, Strength, Consttution, etc)

            // morale

            // loyalty
            
        }

        /// <summary>
        /// Assigns characters to a duty such as Helm, Doctor, Engineer, etc. 
        /// We prodominantly chose based on Rank and Age.
        /// </summary>
        /// <param name="characters"></param>
        private static void AssignDuties(GameObjects.Character character)
        {
            // assign officers first
            //    then do the officers (or maybe just the 1st officer) select their underlings?

            // todo: perhaps we can first generate a duty list, then assign the characters. This way
            // you cant wind up with 2 captains or 2 commanders or Chiefs of departments (eg Chief Engineer, Chief Security Officer, etc)

            // todo: I think we also need a "Supervisor" property which details which NPC (or player in the case of Captain) another NPC reports to in the chain of command.
            // when promoting crew members or when one dies, we may need to modify the chain of command and the "Supervisor" for affected underlings
            // At runtime, the player should be able to demote/promote and reassign duties as he sees fit, however the 1st officer periodically reviews things and can make suggestions to the Captain (player)
            // The first officer may do an evaluation report after every "mission"

        }

        private static void AssignCrewQuarters(GameObjects.Character character)
        {
        }

    }
}
