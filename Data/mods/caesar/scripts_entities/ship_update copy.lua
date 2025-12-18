--lua script test


function OnDie (entityID, data)
		DoEntityMove()
		return 999123
end

function OnDie2 ()
		DoEntityMove()
		print ("22222")
end

function OnDie3(data)
		DoEntityMove()
		DoEntityMove()
		DoEntityMove()
		return 42
end

   