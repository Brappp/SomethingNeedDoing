--DD helper for solo or party of other cousins
--this is the party leader script in a group of cousins. the others would run frenrider

--config -> turn on everything in the dd module, turn OFF bronze coffers

--important variables
fatfuck = 1
number_of_party = 4 --how many poople in party hah well we will check anyhow

function pooplecheck()
	number_of_party = 4
	number_of_party = number_of_party - 1 --index starts at 0 anyways
	for poopy=0,number_of_party-1 do
		if string.len(GetPartyMemberName(poopy)) < 1 then number_of_party = number_of_party - 1 end
		yield("/wait 0.5")
	end
	number_of_party = number_of_party + 1
	yield("Number of poople in poopy ->"..number_of_party)
	number_of_party = number_of_party - 1
	if number_of_party < 0 then number_of_party = 0 end
end
pooplecheck()

--counters
fattack = 0
fanav = 0
samenav = 0
wallitbro = 0
anal_of_passage = 0

rpX = GetPlayerRawXPos()
rpY = GetPlayerRawYPos()
rpZ = GetPlayerRawZPos()


while fatfuck == 1 do
	yield("/wait 1")
	if IsPlayerAvailable() == false then
		yield("/send NUMPAD0")
	end
	if IsPlayerAvailable() then
		--*we should probably check for toad/otter/owl/capybara status and force path to the anal of passage
		yield("/echo Counters -> attack->"..fattack.." nav->"..fanav.." stopnav->"..samenav.." wall->"..wallitbro.." anal->"..anal_of_passage)
		fattack = fattack + 1
		fanav = fanav + 1
		wallitbro = wallitbro + 1
		anal_of_passage = anal_of_passage + 1
		
		if wallitbro > 50 then
			--run in a stright line for 3 seconds once every 50 seconds. this will fix stuck hallway bs with no target on HUD
			yield("/hold W <wait.3.0>")
			yield("/release W")
			wallitbro = 0
			yield("/wait 1")
		end
		
		if anal_of_passage > 180 then --every 3 minutes try to leave the floor just in case we stuck
			if anal_of_passage > 200 then
				fattack = 0
				fanav = 0
				samenav = 0
				wallitbro = 0
				anal_of_passage = 0
			end
			--nemm = GetPartyMemberName(1)
			--yield("/vnav moveto "..GetObjectRawXPos(nemm).." "..GetObjectRawYPos(nemm).." "..GetObjectRawZPos(nemm))
			nemm = "Cairn of Passage"--this works if it exists so we can do this right after trying a party member. it will go to the party member otherwise.
			yield("/vnav moveto "..GetObjectRawXPos(nemm).." "..GetObjectRawYPos(nemm).." "..GetObjectRawZPos(nemm))
			yield("/wait 1")
		end

		if fattack > 5 then 
			--attack stuff
			yield("/bm on")
			shetzone = GetZoneID()
			if shetzone == 561 then yield("/target Death") end --floor 10
			--if shetzone == 561 then yield("/target Death") end --floor 20
			--if shetzone == 561 then yield("/target Death") end --floor 30
			--if shetzone == 561 then yield("/target Death") end --floor 40
			if shetzone == 565 then yield("/target Edda") end --floor 50

			yield("/bmrai on")

			--get thee to next floor
			pooplecheck()
			yield("/target Point")
			yield("/interact")
			yield("/send NUMPAD0")
			yield("/send NUMPAD0")
			fattack = 0
			--also check for dead party members and path to them asap
			for i=0,number_of_party do
				nemm = GetPartyMemberName(i)
				yield("/wait 0.5")
				aitchpee = GetPartyMemberHP(i)
				yield("Party member["..i.."] Name->"..nemm.." HP->"..aitchpee)
				if aitchpee < 5 then
					yield("/echo we need to save "..nemm.."->"..GetObjectRawXPos(nemm).." y "..GetObjectRawYPos(nemm).." z "..GetObjectRawZPos(nemm).."!")
					--yield("/echo we need to save "..GetPartyMemberName(i).."->"..GetPartyMemberRawXPos(i).." y "..GetPartyMemberRawYPos(i).." z "..GetPartyMemberRawZPos(i).."!")
					yield("/vnav stop")
					--yield("/bmrai off")
					yield("/wait 1")
					yield("/vnav moveto "..GetObjectRawXPos(nemm).." "..GetObjectRawYPos(nemm).." "..GetObjectRawZPos(nemm))
					--yield("/vnav moveto "..GetPartyMemberRawXPos(i).." "..GetPartyMemberRawYPos(i).." "..GetPartyMemberRawZPos(i))
					yield("/wait 5")		
				end
			end
			yield("/wait 1")
		end

		if fanav > 30 and samenav < 30 then
			fanav = 0
			nemm = GetTargetName()
			yield("/vnav moveto "..GetObjectRawXPos(nemm).." "..GetObjectRawYPos(nemm).." "..GetObjectRawZPos(nemm))
			yield("/wait 1")
		end

		if math.abs(GetPlayerRawXPos()-rpX) < 3 then samenav = samenav + 1 end
		if math.abs(GetPlayerRawYPos()-rpY) < 3 then samenav = samenav + 1 end
		if math.abs(GetPlayerRawZPos()-rpZ) < 3 then samenav = samenav + 1 end
		
		if samenav > 30 then
			yield("/vnav stop")
			samenav = 0
			yield("/wait 1")
		end

		--we get stuck sometimes should probably stop vnav after a while if we havent moved.
		
		
	end
end