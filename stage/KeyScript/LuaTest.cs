using System;
using System.Collections.Generic;

namespace KeyScript
{
    class LuaTest
    {
        //private class TestNPC
        //{
        //    public LuaInterface.LuaFunction  OnDie;
        //    public delegate void DieHandler();
        //    public delegate int DieHandlerInt(string name);

        //    public DieHandler DoDie;
        //    public DieHandlerInt DoeDieInt;

        //    public void Kill()
        //    {
        //        double i = (double)OnDie.Call()[0];
        //        Debug.WriteLine(i.ToString());

        //        DoDie();
        //        int resultInt = DoeDieInt("42");
        //        Debug.WriteLine(resultInt.ToString());
        //    }
        //}
        ////http://penlight.luaforge.net/packages/LuaInterface/
        ////http://lua-users.org/wiki/LuaInterface
        ////http://github.com/mascarenhas/luaclr
        ////http://www.gamedev.net/community/forums/topic.asp?topic_id=515419
        ////http://lua-users.org/lists/lua-l/2003-10/msg00268.html
        ////http://www.youngcoders.com/net-programming/30025-luainterface-explained.html#post146259
        //LuaInterface.Lua mLua = new LuaInterface.Lua();

        //private void TestLua()
        //{
        //    // file must be re-read via .DoFile() if you'v emade changes.  Thus i was wrong about lua automatically
        //    // knowing a file has changed.  But this is somewhat good in that we can use our archiving exactly as we 
        //    // do with other game data types.... we can update the zip with any changes, then re-grab out of the archive
        //    // Fortunately during actual game runtime, we only have to typically load scripts at the start of a game or level or something
        //    string file = @"E:\dev\c#\KeystoneGameBlocks\Data\pool\scripts\entity_die_test.txt";
        //    mLua.DoFile(file);

        //    LuaInterface.LuaUserData data;

        //    // "MoveEntity" must apparently be public method
        //    mLua.RegisterFunction("DoEntityMove", this, this.GetType().GetMethod("MoveEntity"));
        //    TestNPC monster = new TestNPC();
        //    monster.OnDie = mLua.GetFunction("OnDie");
        //    monster.DoDie = (TestNPC.DieHandler)mLua.GetFunction(typeof(TestNPC.DieHandler), "OnDie2");
        //    monster.DoeDieInt = (TestNPC.DieHandlerInt)mLua.GetFunction(typeof(TestNPC.DieHandlerInt), "OnDie3");
        //    monster.Kill();

        //    // mLua.GetFunction("OnUpdate").Call();

        //    // http://wiki.crymod.com/index.php/Creating_a_New_Entity
        //    // in the above crymod site we see how you have multiple OnDie() in different scripts...
        //    // frankly you dont, they get preceded withd ifferent names like
        //    // DefaultEntity.OnDie()
        //    // Dragon:OnDie()
        //    // Ogre.OnDie()   
        //}


        //--lua script test  "entity_die_test.txt"


        //function OnDie (entityID, data)
        //        DoEntityMove()
        //        return 999123
        //end

        //function OnDie2 ()
        //        DoEntityMove()
        //        print ("22222")
        //end

        //function OnDie3(data)
        //        DoEntityMove()
        //        DoEntityMove()
        //        DoEntityMove()
        //        return 42
        //end

    }
}
