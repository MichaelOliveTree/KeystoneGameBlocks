using System;
using System.Collections.Generic;


namespace Game01.GameObjects
{
    // todo: all our RPG traits should be stored here.  Skills/proficiencies, Advantages/disadvantages, STR, CHR, DEX, CON, INT, etc
    //       Game01 is specific to our game.  It is accessible to the Entity.Scripts as well.
    // todo: with regards to energy weapons and artillary, during "build" mode, i don't need many of those settings. We just care about the halfDamaage, Damage, RoF, energy use, mass, cost, etc.


    // Our KGB uses Helpers.ExtensionMethods GetProperties/SetProperties read/write mechanism 
    // for serializing and deserializing properties to/from Lidgren NetBuffer or to/from a database
    // record or any other format.
    // TODO: Here i am at this stage of the project and im trying to figure out
    //       whether a "gameobject" does not need to inherit from Node.cs. (NOTE: I think not.  Game01.dll needs to be just for game specific rules and need not know about keystone.dll)  I think the Scene Graph
    //       failings are impacting me now.  SceneGraph vs Component Entities vs Game Objects.
    //       We want to have scripted Entities (including Digests) and we also want
    //		 more lightweight gameobjects that we know are never visually represented in a 3D Scene Graph.
    //		 Things like "contract", "sensor contact", "crew station", "city", "weapon link", "power grid link", "radar component", "player", "character", "corporation", 
    //		 We know that the SceneGraph's primary role is for traversal of visible elements in a Transform
    //       hierarchy and for managing other visual and audio elements in the Scene.  It's not for general simulation of game objects.
    //		 GameObjects, can reference Scene Graph Entities, but GameObjects are not actually
    //		 part of the 3D Scene.  This is why things are so confusing.  I think things would be easier
    //		 if our Entity/ModeledEntity was RenderableObject and had a reference to a GameObject
    //		 that was a Star or City or Sensor Contact etc and that GameObject could be scripted.
    //		 But then what about Physics Nodes, Behavior nodes and ScriptNodes (DomainObjects) and  AI nodes?
    //		 Being able to serialize them into a xml db is nice.  Visibility Graph/DAG, Transform Graph/DAG.
    //       Being able to traverse the visibility graph, add renderable nodes to buckets, etc. And the
    //		 hierarchical partitioning is helpful.  GamePlayObjects perhaps need their own structure.
    //		 These can reference an Entity in the SceneGraph?  What does a game like Dwarf Fortress do?
    //		 It doesn't use a SceneGraph, but it simulates many gameObjects. Dwarves, monsters, tiles & resources,
    //		 water simulation, 
    //		 Ugh.  Or maybe just continue with what I'm doing?  It's too late for refactoring/overhauls.  Just get it done.  
    // 		 What does that mean?  It means staying with our current Entity/SG hybrid.  Player is then an Entity.  Vehicle
    //		 is referenced by Player.  Vehicle is a child of Player.  Components (radar, planetary survey array, launchers, computers, etc) as Entities as well.  
    //		 but they can be entities without ModeledEntities.  So maybe the question is, can we push
    //		 our entities into a DB like sqlite?  First, AGAIN, we must start with a Player object. Server must spawn it 
    //		 for the client to re-create.  
    //
    //		 Let's start by putting Player object as type of Entity directly under AppMain and/or AppMain.CoreClient
    //		 Let's have our universe generator, supply a spawnLocation entity that was specified by user during universe
    //		 gen configuration.  Then the spawnLocation can spawn a Player from server side to Client.  When a user connects
    //		 the game checks for a spawn point for that player and executes it?  The spawnpoint has a script that runs once
    //		 when the entity is activated by client connection event.  But what about when user saves, exits and resumes?
    //		 This is an issue with respect to saving/loading vs scene generation.  It also suggests that client is allowed
    //		 to modify the Scene database.  This is why it would be good to have seperate db's for Scene vs SavedState
    //			
    //		 Look, our Scene XMLDB is being used as a fully fledged game DB.  But this is going to stifle us as we
    //       look to add new GameObjects to the simulation.  The question is can our hierarchical structure be saved
    //		 to SQL db easily?  We investigated this some time ago and it was "no."  But we're trying to get Player object
    //		 instantiated and referenced when player "Connects" to loopback.  That's it.   How is the Player object read in,
    //		 recognized, referenced on Client after connecting to loopack and loading in Scene XMLD.
    //
    //		 What if we keep using PREFABS as XMLDB's but then reference those prefab relative paths in our sqlite db?
    //		 So what if our Player selects Vehicle, that vehicle is stored along with the playerID in db.	
    //		 A second command is sent from loopback to instantiate selected vehicle?	
    //
    //       1) app main
    //		 2) netClient created connect to server
    //		 3) scene to load passed in
    //		 4) scene is loaded and shared between loopback client and loopback server
    //		 5) _lookup user name and query db for user player vehicle
    //		 6) 	spawn command from server to client (or should we think of spawn more as "attaching" to loaded Vehicle? We need an event when player owned vehicle is instantiated)
    //				do we have an event for each Entity on instantiation where we could then query a bool IsLocalPlayer?
    //
    //		 Client
    //		 |_Connection
    //		 |
    //		 Player
    //          |_User
    //			|_Vehicle
    //			|_CharacterProfile
    //
    //		
    /// <summary>
    /// Dec.29.2012
    /// A game object is differentiated from an Entity because it is not derived
    /// from Node.  A node is a something that is interactable with, within the scene
    /// by other scene node elements (i.e. Enities).  This includes triggers, sensor volumes,
    /// and characters and walls and rocks, etc.
    /// Game objects on the other hand, are interactable only in an abstract sense. Waypoints
    /// can guide Entities. Contracts can create a relationship between entities and their
    /// behaviors.  Sensor contacts provide a nugget of information with which users or ai 
    /// behaviors can make decisions upon.  
    /// Can GameObjects be serialized/deserialized?
    /// Yes.  They are stored permanentaly in serverside databases. For loopback
    /// the database is also client side but is managed by the loopback server.
    /// They may be cached client side.  Not sure yet.
    /// 
    /// GameObject derived types are well defined.  They have no "custom properties"
    /// as Entities do through scripted domainobject.  Thus they are friendly to 
    /// database storage.
    /// 
    /// GameObjects can be referenced by Entity scripts but themselves are not scriptable.
    /// They are also not shareable.  All GameObjects are unique.
    /// 
    /// Dec.26.2012
    /// A game object has no world representation or need to interact in the game space.
    /// So that means it is not a trigger, sensor, zone, modeled entity, etc.
    /// It is a game specific construct however which can be serialized over the wire
    /// and as such has properties both intrinsic and custom.  
    /// Since these constructs are game specific, the keystone.dll game engine has no
    /// need to know what they are.
    /// Orders, Sensor Contacts, Contracts, Spy Data, etc.  In a way, GameObjects can be non 
    /// concrete, where as an Entity is always concrete tangible physical or at least
    /// virtually physical as in case of triggers and proximity sensors and such.
    /// The real undecided issue is whether a GameObject can host a DomainObject and be
    /// scripted.  I'm thinking for now at least no.  A GameObject can be used by Scripted 
    /// Entities but by themselves, cannot.
    /// 
    ///  other game objects I think might be match, player table, players' captain avatar,
    ///  match result, server info (although perhaps some of these are not gameobjets
    ///  but DabaseObjects which GameObject also inherits)
    ///  biosphere stats, political stats, culture, government, and such.  
    ///       
    /// </summary>
    /// 

    //
    // A hierarchical algorithm for generating not just individual characters, but families
    // and family histories that now can take advantage of this hierarchy history
    // wealth, losing inheritance, etc.
    // We can make a fairly complex "universe generation" type algorithm for creating
    // populations on many worlds and where there are some "keystone" families that are
    // focal points and that can be npcs in game as opposed to nameless faceless 
    // characters that have 0 degrees of seperation between anyone of any significance.
    //

    //
    // Serenity MUSH website is amazing!
    // http://www.serenitymush.com/wiki/index.php/Category:Bonaventure
    //
    // I love how they have detailed backgrounds on characters and the use real photos
    // and how they have actual companies and hospitals complete with board of directors
    // and it really sells you into the idea of being in that universe.
    //
    // so this is what we are talking about when we talk about games fit for adults
    // 
    // We can also make our database look very much like that WIKI instead of some old school
    // monochrome display, it's wiki style but still elegant and simple and our 
    // hierarchical character generator can automatically build in all the cross reference links.
    //  - our wiki can be a real live wiki and does not need to be embedded into the game.
    //      - 
    // WIKI GENERATION C#  - http://stackoverflow.com/questions/10533818/programmatically-create-media-wiki-pages-using-c-sharp
    //  http://en.wikipedia.org/wiki/User:Svick/LinqToWiki
    //      - https://github.com/svick/LINQ-to-Wiki
    //          "Where LINQ to Wiki really shines, though, are queries: If you wanted to get the names of all pages in Category:Mammals of Indonesia, you can do:"
    //  var pages = (from cm in wiki.Query.categorymembers()
    //         where cm.title == "Category:Mammals of Indonesia"
    //       select cm.title)
    //      .ToEnumerable();
    //
    //  So our site can be real wiki, but the game itself can allow for queries to our wiki
    //
    // 
    //  http://sourceforge.net/projects/dotnetwikibot/

    // Edu-Ware's Space II, Traveller Inspired RPG on the Apple ][   
    // watch this video, it has some interesting crew management aspects where you can
    // view their psychological profiles and service records, how many years left in their
    // service contract, etc
    // http://www.youtube.com/watch?v=her2mF0WuTQ

    // I love the concept of players really needing to make choices that matter with respect to
    // how their games progress.  It's a lot like the Zomboid style games and survival games.
    // There is no "wining" per se.  There's just survival.
    // http://en.wikipedia.org/wiki/Space_(series)
    // Mullich wrote the sequel, Space II, as an exercise in risk-benefit analysis, 
    // as the player's character is presented with dangerous options throughout the game, 
    // and the player must determine whether the potential rewards are worth the possible risks.
    // 
    // later Edu-Ware replaced the Space series with Empire trilogy. 
    // http://www.imdb.com/title/tt0446690/

    // let's play Empire 3: Armageddon 
    // http://www.youtube.com/watch?v=RAp6Mq39GzQ


    // sims 3 castaway stories
    // i like the exterior would make good for tactics aspect of First In away missions
    // http://www.youtube.com/watch?v=IanCxBRChmU&feature=bf_next&list=UUbuEeYtxR1NvmYbrbM8ujYQ
    // also i think it could be fine with simple tiles and loopix vegetation
    // but additionally, their can be underground caverns as well as elevated caves in the cliffs/mesas
    public class Character // todo: maybe just NPC
    {
        private int mGender = 0;

        private string mFirstName;
        private string mMiddleName;
        private string mLastName;

        // See KeyCommon.DatabaseEntities.Character.cs

        // How many men are in a squad? company, platoon, legion, etc?
        // http://answers.yahoo.com/question/index?qid=20080424225740AAV9Euu
        //Let us start with the basics:

        //A fire team of 3-5 soldiers is the most basic unit
        //There are 2-4 teams in a squad
        //There are 2-4 squads in a platoon
        //There are 2-6 platoons in a company
        //There are 2-6 companies in a battalion
        //There are 2-5 battalions in a brigade
        //There are 2-4 brigades in a division

        //You can do the math on this, but roughly this means:

        //Fireteam: 4-5
        //Squad: 8-16
        //Platoon: 25-60
        //Company: 70-250
        //Battalion: 300-1000
        //Brigade: 3000-5000
        //Division: 10,000-20,000

        //As for legions, those have not officially existed since the Roman empire.

        //                Chain of command
        //Unit 	        Soldiers 	Typical Commander
        //fireteam 	        4 	                NCO
        //squad/section 	8–13 	            squad leader
        //platoon 	        26–64 	            platoon leader
        //company 	        80–225 	            captain/major
        //battalion 	    300–1,300 	        lieutenant colonel/colonel
        //regiment/brigade 	3,000–5,000 	lieutenant colonel/colonel/brigadier/brigadier general
        //division 	        10,000–15,000 	    major general
        //corps 	        20,000–45,000 	    lieutenant general
        //field army 	    80,000–200,000 	    general
        //army group 	    400,000–1,000,000 	general of the army
        //army region 	    1,000,000–3,000,000  field marshal
        //theater 	        3,000,000–10,000,000 generalissimo

        //              Common anglophone military ranks
        //              Navies 	Armies 	Air forces
        //                          OFFICERS
        //Admiral of the fleet 	Marshal/field marshal 	Marshal of the Air Force
        //Admiral 	            General 	            Air Chief Marshal
        //Vice Admiral 	        Lieutenant General 	    Air Marshal
        //Rear Admiral 	        Major General 	        Air Vice Marshal
        //Commodore 	        Brigadier 	            Air commodore
        //Captain 	            Colonel 	            Group captain
        //Commander 	        Lieutenant colonel 	    Wing commander
        //Lieutenant commander 	Major /commandant 	    Squadron leader
        //Lieutenant 	        Captain 	            Flight lieutenant
        //Sub-lieutenant 	    Lieutenant 	            Flying officer
        //Ensign 	            2nd lieutenant 	        Pilot officer
        //Midshipman 	        Officer cadet 	        Officer cadet
        //              SEAMAN, SOLDIER, AND AIRMAN
        //Warrant officer 	    Sergeant major/Warrant officer 	Warrant officer
        //Petty officer 	    Sergeant 	            Sergeant
        //Leading seaman 	    Corporal 	            Corporal
        //Seaman 	            Private 	            Aircraftman

        // advantages/disadvantages and skills/proficiences to just the ones in Space, First In, and Traveller

        // todo: what about making all this custom properties defined and initialized in the scripts? we then still have the issue of formatting it in the quicklook but we create the HTML in css side scripts anyway
        // todo: however, when we have LOTS of npcs that reside on planets or space stations, we just want to be able to grab their data from a proper database right?
        public string FirstName { get { return mFirstName; } set { mFirstName = value; } }
        public string MiddleName { get { return mMiddleName; } set { mMiddleName = value; } }
        public string LastName { get { return mLastName; } set { mLastName = value; } }
        public int Gender { get { return mGender; } set { mGender = value; } }
    }
}
