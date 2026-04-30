# Karma Brainstorming

A living scratchpad for game ideas. Nothing here is a promise; good ideas can graduate into `AGENTS.md`, issues, prototypes, or design docs later.

## North Star

Karma should feel like a social sandbox where every choice leaves a visible trail: reputation, rumors, bounty pressure, alliances, rescues, betrayals, and weird little emergent stories.

## Event Categorization / Theme Tags

Events should be categorized so we can reuse the same mechanics across different skins/settings without accidentally mixing theme-specific flavor. Example: a western-only “stagecoach robbery” should not appear in a space-station theme unless it has a space-flavored equivalent.

### Suggested Event Metadata

Each event idea can carry tags like:

- **Mechanical category**: what the event does in gameplay.
  - `combat`, `rescue`, `law`, `bounty`, `trade`, `supply`, `theft`, `social`, `rumor`, `quest`, `structure`, `travel`, `faction`
- **Theme fit**: where the event flavor belongs.
  - `theme:any`, `theme:western`, `theme:space`, `theme:post-apoc`, `theme:fantasy`, etc.
- **Tone**: how serious/silly/dramatic it should feel.
  - `tone:serious`, `tone:funny`, `tone:weird`, `tone:grim`, `tone:heroic`
- **Scale**: how much of the world it touches.
  - `scale:personal`, `scale:local`, `scale:global`
- **Karma vector**: what moral/social pressure it creates.
  - `karma:helpful`, `karma:harmful`, `karma:lawful`, `karma:chaotic`, `karma:selfish`, `karma:generous`

### Theme-Agnostic Event Pattern

Prefer designing the **mechanical event pattern** first, then adding theme-specific names/flavor.

Example pattern:

- **Mechanical pattern**: vulnerable transport carrying valuables appears; players can protect it, rob it, or recover stolen goods.
- **Western flavor**: stagecoach robbery.
- **Space flavor**: cargo shuttle distress signal.
- **Post-apoc flavor**: armored water convoy.
- **Fantasy flavor**: merchant caravan ambush.

### Event Category Buckets

- **Core / Theme-Agnostic**
  - Rescue downed player
  - Return stolen/lost item
  - Public theft witnessed by NPCs
  - Supply drop scramble
  - Duel/challenge
  - Bounty target pursuit
  - Shop price/reputation changes

- **Western Theme**
  - Stagecoach robbery
  - Saloon duel
  - Ranch/livestock theft
  - Train hold-up
  - Sheriff posse pursuit
  - Frontier clinic triage

- **Space Theme**
  - Cargo shuttle distress beacon
  - Airlock sabotage
  - Contraband scan checkpoint
  - Reactor power outage
  - Derelict salvage claim
  - Med-bay emergency revive

- **Post-Apoc Theme**
  - Water convoy raid
  - Generator fuel shortage
  - Scrap trader ambush
  - Mutant/wildlife incursion
  - Settlement gate lockdown

- **Fantasy Theme**
  - Merchant caravan ambush
  - Shrine corruption cleanse
  - Cursed relic contraband
  - Guild bounty board
  - Healer hut revive

### Working Rule

When adding a new event, write it as:

```text
Event Name
- Mechanical category:
- Theme tags:
- Core pattern:
- Theme flavor:
- Karma hooks:
- Prototype/test idea:
```

This keeps us from hard-coding one theme into systems that should stay reusable.

## Playable Event Prototype Ideas

- **Duel at High Noon**
  - Categories: `combat`, `social`, `law`; Theme tags: `theme:western`, adaptable to `theme:any` as “formal challenge.”
  - Two players agree to a duel, nearby players get a small world event ping, and attacks between duelists avoid normal outlaw karma penalties.
  - Add a countdown/stance moment before shots count.

- **Rescue Under Fire**
  - Categories: `rescue`, `combat`; Theme tags: `theme:any`.
  - A player gets downed in a dangerous zone.
  - Another player can rescue them, but doing so exposes both to risk and gives a visible heroic karma event.

- **Karma Break Loot Trail**
  - Categories: `theft`, `karma`, `social`; Theme tags: `theme:any`.
  - When a player Karma Breaks, their dropped items create a small public moral dilemma.
  - Looting hurts reputation; returning loot gives a visible redemption event.

- **Wanted Chase Route**
  - Categories: `law`, `bounty`, `combat`; Theme tags: `theme:any`, with western/space variants.
  - Warden issues Wanted.
  - Target has a head start, bounty hunters get clues/rumors instead of perfect tracking.
  - Capture/downing triggers bounty and Wanted-resolution events.

- **Contraband Checkpoint**
  - Categories: `law`, `theft`, `stealth`; Theme tags: `theme:space`, `theme:any`.
  - Law NPCs or players can detect contraband in a checkpoint zone.
  - Smugglers can sneak around, bribe, stash goods, or sprint through and become Wanted.

- **Supply Drop Scramble**
  - Categories: `supply`, `travel`, `combat`; Theme tags: `theme:any`.
  - Global supply drop event with a visible landing zone.
  - First claim wins, but claiming near witnesses may create rumor/karma consequences depending on item type.

- **Clinic Triage Moment**
  - Categories: `rescue`, `trade`; Theme tags: `theme:any`.
  - Downed players near clinic can be revived for scrip.
  - If they cannot pay, another player can cover cost for karma/favor.

## Social Systems

- **Rumor Network**
  - NPCs repeat simplified summaries of recent events: thefts, rescues, betrayals, duels, bounty claims.
  - Rumors should be imperfect: “someone saw Mara’s supplies vanish near the greenhouse.”

- **Reputation Memory**
  - NPCs remember broad player patterns: generous, violent, sneaky, reliable, chaotic.
  - Dialogue options/prices subtly shift based on this reputation.

- **Posse Identity**
  - Posses can earn a temporary group reputation: trusted crew, outlaw gang, clinic helpers, etc.
  - Group actions affect every member lightly, but direct actor gets the strongest shift.

- **Public Witnesses**
  - Crimes and heroics only become public if witnessed by NPCs/players, cameras, or rumor sources.
  - Creates interesting stealth/social gameplay.

## World/Event Ideas

- **Traveling Merchant**
  - Categories: `trade`, `travel`, `social`; Theme tags: `theme:any`.
  - Appears after supply events or high local activity.
  - Sells rare utility items but remembers who robs or protects them.

- **Power Outage**
  - Categories: `structure`, `faction`, `theft`; Theme tags: `theme:space`, `theme:post-apoc`, adaptable.
  - A district loses lights/doors/security.
  - Players can repair for civic karma or exploit for theft.

- **Faction Request Board**
  - Categories: `quest`, `faction`, `trade`; Theme tags: `theme:any`.
  - Small rotating objectives: deliver parts, escort NPC, recover stolen goods, sabotage rival facility.
  - Completing one changes local station state.

- **Heat Zones**
  - Categories: `combat`, `law`, `faction`; Theme tags: `theme:any`.
  - Repeated combat raises local heat.
  - High heat attracts law NPC patrols, bounty hunters, or shuts down shops temporarily.

- **Safehouse / Hideout**
  - Categories: `law`, `bounty`, `theft`; Theme tags: `theme:any`.
  - Outlaws can stash contraband or reduce heat.
  - Saints/Wardens can discover/raid it through clues.

## Items & Interaction Ideas

- **Scanner**: reveals contraband or hidden dropped items in a small radius. Good for space/scifi; can become “Wanted poster clue” or “tracking kit” in western.
- **Disguise Kit**: temporarily hides Wanted marker from NPCs, weaker against players.
- **Signal Flare**: calls a supply drop but announces location publicly. Theme variants: flare gun, distress beacon, magic signal.
- **Evidence Bag**: lets Wardens collect contraband for bounty/reputation.
- **Portable Shield**: defensive item for rescues or duels.
- **Rumor Pamphlet**: lets player amplify or distort one recent event.

## Prototype/Testing Candidates

Add these as in-game prototype scenarios when systems are ready:

- Warden chase with moving Wanted target.
- Witnessed vs unwitnessed theft.
- Returning Karma Break loot to original owner.
- Clinic revive paid by a third party.
- Posse reputation after group rescue or group crime.
- Shop prices changing after saint/scourge reputation changes.
- Local chat falloff and inside-building muffling.
- Mount theft / occupied mount rejection / parking near station.
- Theme-filtered event spawn list: prove western-only events do not spawn in space mode and vice versa.

## Tone Ideas

- Keep the world a little funny and human: bureaucratic bounty notices, overdramatic clinic staff, NPCs gossiping too confidently.
- Make consequences readable but not preachy. Karma is a social force, not a morality lecture.
- Favor emergent “wait, did you see that?” moments over long scripted cutscenes.

## 50 Event Seed Ideas

These are categorized by core mechanic first, with theme notes where useful.

### Law / Bounty / Justice

1. **Wanted Notice Posted**
   - A high-negative-karma player gets publicly marked as Wanted.
   - Western: sheriff notice board. Space: station security bulletin.

2. **Bounty Hunter Arrival**
   - NPC or player bounty hunters arrive in a hot zone after repeated crimes.

3. **False Accusation**
   - A player can accuse someone of a crime; witnesses/evidence decide whether it sticks.

4. **Contraband Sweep**
   - Law NPCs temporarily inspect players passing through a checkpoint.

5. **Jailbreak / Detention Escape**
   - A captured player can be broken out, legally released, or abandoned.

6. **Evidence Recovery**
   - Evidence drops at a crime scene; returning it helps justice, destroying it helps criminals.

7. **Deputized Posse**
   - Good-reputation players can temporarily join a lawful hunt for a Wanted target.

8. **Corrupt Law Offer**
   - A law NPC offers to ignore contraband for a bribe, creating a karma dilemma.

### Rescue / Survival / Medical

9. **Downed Stranger**
   - A wounded NPC/player appears nearby; helping costs time/resources but grants reputation.

10. **Clinic Overload**
   - Too many injured arrive at once; players choose who gets help first.

11. **Medicine Shortage**
   - Clinic needs supplies. Delivering medicine improves local stability.

12. **Dangerous Extraction**
   - Someone is trapped in a hazardous area and must be escorted out.

13. **Paid Revive Request**
   - A downed player cannot afford clinic revival; others can cover the fee.

14. **Rescue Betrayal**
   - A rescued target turns out to be Wanted or carrying stolen goods.

### Trade / Economy / Supplies

15. **Supply Drop Scramble**
   - A valuable cache lands publicly; first claimant wins, witnesses judge the action.

16. **Traveling Merchant**
   - A rare vendor arrives with limited stock and remembers who helps or robs them.

17. **Price Surge**
   - Local shop prices rise after violence, shortage, or faction instability.

18. **Black Market Pop-Up**
   - A temporary illegal vendor sells contraband or suspicious tools.

19. **Delivery Contract**
   - Carry a package across the map. Others may steal, protect, or inspect it.

20. **Spoiled Shipment**
   - Supplies are damaged unless delivered quickly.

21. **Auction Lot**
   - Players bid or barter for a rare item; theft or fraud can disrupt it.

### Theft / Contraband / Crime

22. **Pickpocket Witnessed**
   - Theft is only punished if witnessed or discovered.

23. **Stolen Goods Trail**
   - Stolen items leave clues as they move between players.

24. **Contraband Cache**
   - Hidden goods appear; players can report, steal, move, or destroy them.

25. **Safehouse Discovered**
   - A criminal hideout is revealed through rumors or tracking.

26. **Fence Request**
   - An NPC offers to launder stolen goods for a cut.

27. **Framed with Contraband**
   - Someone plants illegal goods on another player.

28. **Heist Opportunity**
   - A guarded vault/warehouse/cargo bay opens briefly.

### Social / Rumor / Reputation

29. **Rumor Starts**
   - NPCs begin spreading a simplified version of a recent player action.

30. **Rumor Correction**
   - Players can provide evidence to correct or worsen a rumor.

31. **Public Apology**
   - A player can repair reputation after harm by paying, returning items, or apologizing publicly.

32. **Favor Called In**
   - An NPC asks a player they trust for help, creating a private quest.

33. **Rivalry Declared**
   - Two players or factions become known rivals, changing NPC reactions.

34. **Community Vote**
   - Locals vote on punishment, reward, or resource allocation based on recent events.

35. **Witness Protection**
   - A witness to a crime needs escort before they can testify/report.

### Combat / Conflict / Duels

36. **Formal Duel Challenge**
   - Players agree to fight under special rules, avoiding normal crime penalties.

37. **Ambush Warning**
   - A rumor hints that enemies are waiting along a route.

38. **Heat Zone Escalation**
   - Repeated combat makes an area dangerous and draws law/faction response.

39. **Mercenary Contract**
   - A faction pays for protection, intimidation, or recovery.

40. **Revenge Attack**
   - An NPC/faction retaliates against a player who harmed them earlier.

### Structure / World State

41. **Power Outage**
   - Lights/security/doors fail. Players can repair or exploit the blackout.

42. **Generator Sabotage**
   - A key structure loses integrity and changes local station/town state.

43. **Bridge / Airlock Failure**
   - A route is blocked until repaired or bypassed.

44. **Greenhouse Contamination**
   - Food/medicine production is threatened; repair or theft choices matter.

45. **Station Lockdown**
   - A region locks down after violence or contraband discovery.

46. **Public Notice Board Update**
   - New bounties, quests, warnings, and rumors appear based on server history.

### Travel / Mounts / Exploration

47. **Mount Theft**
   - A parked mount can be stolen, returned, chopped, or reported.

48. **Stranded Traveler**
   - Someone needs a ride, escort, fuel, or repair kit.

49. **Derelict Salvage / Abandoned Wagon**
   - A discovered wreck contains loot, danger, or evidence.

50. **Route Patrol**
   - A patrol/convoy moves through the map, creating protection or ambush opportunities.

## Additional Event Seed Pool

More raw event ideas for later ranking. These intentionally overlap in mechanics sometimes; ranking/refinement can merge the best versions.

### Law / Bounty / Justice Continued

51. **Anonymous Tip**
   - A player receives a clue pointing toward contraband, stolen goods, or a Wanted target.

52. **Bounty Board Refresh**
   - New bounties appear based on recent crimes, faction anger, or player reputation.

53. **Warden Audit**
   - Wardens are challenged to prove a Wanted mark was justified.

54. **Escaped Prisoner**
   - A detained NPC/player escapes and nearby players can help, ignore, or recapture them.

55. **Public Trial**
   - A major crime triggers a timed community/legal decision event.

56. **Tampered Evidence**
   - Evidence at a crime scene can be altered, making justice harder.

57. **Bounty Rivalry**
   - Two bounty hunters compete for the same target; cooperation or betrayal possible.

58. **Amnesty Window**
   - Wanted players can turn themselves in for reduced penalty during a short window.

59. **Marshal Inspection**
   - A powerful law NPC visits; crime has higher consequences nearby.

60. **Unpaid Fine**
   - A player with outstanding penalties gets shop restrictions or NPC pressure.

61. **Witness Bribery**
   - A witness can be paid to stay quiet or rewarded for speaking up.

62. **Protection Order**
   - A vulnerable NPC/player is temporarily protected; attacking them has harsh consequences.

63. **Bounty Escalation**
   - A Wanted player who keeps offending gets a larger reward and wider alert radius.

64. **Mistaken Identity**
   - A disguised or framed player is blamed until evidence clears them.

65. **Citizen Arrest**
   - High-trust players can restrain a criminal briefly until law arrives.

### Crime / Heists / Stealth Continued

66. **Locked Strongbox**
   - A loot box needs time/tools to open, attracting attention.

67. **Silent Break-In**
   - Enter a restricted structure unnoticed; witnesses create a rumor trail.

68. **Alarm Triggered**
   - A theft/structure sabotage trips an alarm and starts a short escape window.

69. **Inside Job**
   - A trusted player is offered a betrayal opportunity against their faction/posse.

70. **Smuggler Route**
   - A hidden route opens for contraband carriers.

71. **Fence Double-Cross**
   - A black market NPC cheats the player unless they have enough reputation.

72. **Stash Decay**
   - Hidden contraband becomes less valuable or more risky over time.

73. **Marked Bills / Tagged Cargo**
   - Stolen currency/items can be tracked if spent too quickly.

74. **Decoy Contraband**
   - A fake illegal package is used to bait thieves or corrupt law.

75. **Snitch in the Crew**
   - A posse member/NPC leaks criminal plans.

76. **Safe Cracker Needed**
   - A heist requires a specific tool, perk, or player role.

77. **Getaway Route Blocked**
   - After theft, a bridge/airlock/gate closes, forcing improvisation.

78. **Loot Too Heavy**
   - Valuable cargo slows the carrier until dropped, split, or mounted.

79. **Secret Auction**
   - Stolen goods are sold at an illegal auction.

80. **Forgery Attempt**
   - Fake permits/passes let players bypass checks but risk exposure.

### Rescue / Medical / Disaster Continued

81. **Triage Choice**
   - Multiple downed targets; not everyone can be saved without help.

82. **Contagion Scare**
   - A sick NPC/player creates a quarantine zone until supplies arrive.

83. **Emergency Beacon**
   - Someone calls for help; responders risk ambush or gain reputation.

84. **Missing Medic**
   - A clinic worker disappears and must be found to restore revive services.

85. **Field Surgery**
   - A risky revive can be attempted away from clinic with tools.

86. **Blood Debt**
   - A rescued NPC later returns a favor unexpectedly.

87. **Rescue Trap**
   - A fake distress call lures helpful players into danger.

88. **Evacuation Route**
   - Escort civilians/NPCs through a dangerous area.

89. **Collapsed Tunnel / Breached Hallway**
   - Players must dig/repair/open a path to trapped people.

90. **Critical Supply Choice**
   - One medicine shipment can help clinic, faction, or black market.

91. **Life Support Failure**
   - Space flavor: repair oxygen/power before NPCs suffer.

92. **Plague Wagon / Quarantine Shuttle**
   - Theme variant transport event with moral choices about helping vs avoiding.

93. **Mercy Request**
   - An NPC asks for help ending suffering or finding treatment, creating a karma dilemma.

94. **Rescuer Reputation Check**
   - Low-reputation rescuers may be distrusted by victims.

95. **Hospital Theft**
   - Stealing from clinic/medbay is profitable but socially damning.

### Trade / Economy / Crafting Continued

96. **Resource Rush**
   - A rare material appears, pulling players into competition.

97. **Shopkeeper Robbed**
   - A vendor loses inventory; players can recover or exploit the shortage.

98. **Trade Caravan Delay**
   - Shops run out unless a route is cleared/protected.

99. **Counterfeit Goods**
   - Cheap items may fail or cause reputation damage if resold.

100. **Repair Contract**
   - A faction pays for repairing public structures.

101. **Luxury Shipment**
   - Valuable but nonessential goods create greed/social judgment.

102. **Salvage Rights Dispute**
   - Two groups claim the same wreck/cargo.

103. **Vendor Favor Discount**
   - A vendor offers a secret discount to players who helped them before.

104. **Debt Collector**
   - An NPC seeks payment from a player or faction.

105. **Shared Investment**
   - Players pool resources to upgrade a structure/shop/clinic.

106. **Broken Tool Recall**
   - A batch of items malfunctions until repaired or returned.

107. **Insurance Scam**
   - Someone asks players to fake damage/theft for payout.

108. **Market Crash**
   - A commodity drops in value after oversupply or scandal.

109. **Rare Blueprint Found**
   - Players choose to share, sell, or hide crafting knowledge.

110. **Barter Festival**
   - A temporary event encourages trades and social interactions.

### Social / Reputation / NPC Life Continued

111. **NPC Birthday / Celebration**
   - Gifts or disruptions affect reputation with a community.

112. **Public Performance**
   - Players can entertain, heckle, sabotage, or protect an event.

113. **Argument in the Street**
   - Two NPCs fight; players mediate or escalate.

114. **Lost Child / Lost Drone**
   - Find and return a vulnerable companion/entity.

115. **Rival Faction Propaganda**
   - Posters/broadcasts can be spread, removed, or altered.

116. **Gossip Duel**
   - Players compete socially by spreading/defusing rumors.

117. **Reputation Milestone Ceremony**
   - Saints/Scourges/Wardens get public recognition or backlash.

118. **Awkward Reunion**
   - NPCs with history meet; player choices shape their relationship.

119. **Favor Chain**
   - Helping one NPC causes another to request help later.

120. **Community Feast / Ration Day**
   - Food/resource distribution can be fair, corrupt, or stolen.

121. **Apology Gift Delivery**
   - Carry a peace offering between rivals; can be tampered with.

122. **Secret Admirer / Entanglement Hook**
   - Social relationship event that can become rumor material.

123. **NPC Strike**
   - Workers stop providing services until grievances are resolved.

124. **Popularity Contest**
   - Temporary leaderboard for helpfulness, wealth, intimidation, or chaos.

125. **Public Debate**
   - Players influence policy/faction direction through actions or reputation.

### Combat / Danger / Rivalry Continued

126. **Territory Skirmish**
   - Factions fight over a location; players tip the outcome.

127. **Bandit / Pirate Raid**
   - Theme-specific hostile raid on a shop, convoy, or clinic.

128. **Champion Challenge**
   - A strong NPC challenges high-reputation players.

129. **Training Accident**
   - Sparring goes wrong and becomes a rescue/reputation event.

130. **Weapon Malfunction**
   - A weapon/tool fails during combat unless maintained.

131. **No-Kill Contract**
   - Resolve conflict without downing anyone for better reward.

132. **Bodyguard Job**
   - Protect an NPC/player through a dangerous route.

133. **Revenge Posse**
   - Victims of earlier crimes organize retaliation.

134. **Peacekeeping Patrol**
   - Players earn karma by preventing fights, not winning them.

135. **Hazard Herd / Drone Swarm**
   - Non-human threat crosses the map, disrupting plans.

136. **Duel Spectators**
   - Crowd reacts; cheating during a duel triggers reputation damage.

137. **Hostage Standoff**
   - Players negotiate, storm, sneak, or trade for release.

138. **Brawl Spillover**
   - A small fight damages nearby structures/vendors.

139. **Ceasefire Window**
   - A temporary truce rewards cooperation and punishes aggression.

140. **Target Misfire**
   - Friendly fire creates apology, revenge, or justice events.

### World State / Structures / Environment Continued

141. **Weather Hazard**
   - Dust storm, radiation wave, meteor shower, blizzard, etc.

142. **Water / Oxygen / Fuel Leak**
   - Critical resource drains until repaired.

143. **Door Control Failure**
   - Doors/gates lock open or closed, changing routes.

144. **Security Camera Blind Spot**
   - Temporary stealth opportunity appears.

145. **Construction Project**
   - Players donate/haul/repair to build a new structure.

146. **Structure Ownership Dispute**
   - Factions or players contest who controls a place.

147. **Sabotage Investigation**
   - After structure damage, clues identify suspects.

148. **Public Works Reward**
   - Repairing neglected infrastructure grants community-wide benefits.

149. **Environmental Cleanup**
   - Remove contamination/trash/debris for reputation and access.

150. **Hidden Room Revealed**
   - A repaired/sabotaged structure exposes a secret area.

151. **Generator Overload Choice**
   - Divert power to clinic, shops, security, or entertainment.

152. **Fire / Plasma Leak**
   - Spreading hazard forces quick rescue/repair decisions.

153. **Bridge Toll Dispute**
   - NPC/faction charges passage; players can pay, negotiate, or break through.

154. **Station/Town Festival Setup**
   - Help prepare an event; sabotage changes social outcomes.

155. **Emergency Broadcast**
   - A global message points players toward a crisis or opportunity.

### Travel / Exploration / Discovery Continued

156. **Shortcut Found**
   - A hidden path opens; revealing it helps community, hiding it helps smugglers.

157. **Broken-Down Mount**
   - Repair, steal parts, tow, or abandon it.

158. **Fuel Stop Ambush**
   - A travel aid location becomes risky.

159. **Map Fragment**
   - Clues combine into a hidden stash/derelict/camp.

160. **Pilgrim / Settler Escort**
   - Escort peaceful travelers for reputation.

161. **Lost Cargo Signal**
   - Track a signal to loot, danger, or a moral choice.

162. **Toll Gate / Checkpoint Jam**
   - Players can clear, bribe, sneak, or escalate.

163. **Race Challenge**
   - Mount/vehicle race with betting and sabotage risks.

164. **Exploration Claim**
   - First discovery of a location gives naming/reputation rights.

165. **Route Rumor**
   - NPCs mention a safer/dangerous route based on recent events.

166. **Smuggler Tunnel Collapse**
   - Criminal shortcut becomes rescue/repair dilemma.

167. **Beacon Calibration**
   - Repair navigation beacon to improve travel/supply drops.

168. **Wandering Threat**
   - A dangerous entity patrols between regions.

169. **Abandoned Camp**
   - Evidence of previous players/NPCs creates story hooks.

170. **Riderless Mount / Drifting Shuttle**
   - A mount/vehicle appears without owner; finding owner vs keeping it.

### Theme-Specific Variant Seeds

171. **Stagecoach Robbery**
   - Western transport heist; maps to cargo shuttle/water convoy in other themes.

172. **Train Hold-Up**
   - Moving route event with boarding, defending, robbing, or stopping.

173. **Saloon Card Cheat**
   - Western social/crime event around gambling and reputation.

174. **Cattle Rustling**
   - Western livestock theft; maps to drone herd/resource animals.

175. **Sheriff Election**
   - Western/local politics; maps to station council vote.

176. **Airlock Blackmail**
   - Space-specific threat involving access, oxygen, or isolation.

177. **Reactor Scram**
   - Space/station power crisis with faction choices.

178. **Derelict Boarding Claim**
   - Space salvage rights and danger event.

179. **Alien Artifact / Cursed Relic**
   - Theme variant of dangerous valuable contraband.

180. **Meteor Breach**
   - Space environmental disaster; repair/rescue race.

181. **Water Baron Demand**
   - Post-apoc faction controls water access.

182. **Scrap Storm**
   - Post-apoc hazard exposes salvage and danger.

183. **Mutant Nest / Wildlife Den**
   - Clear, relocate, exploit, or protect dangerous creatures.

184. **Radio Tower Hijack**
   - Broadcast rumors, propaganda, or emergency signals.

185. **Fuel Convoy Dispute**
   - Post-apoc transport protection/raid event.

186. **Cursed Shrine**
   - Fantasy structure event: cleanse, exploit, or hide corruption.

187. **Guild Bounty Review**
   - Fantasy/legal equivalent of bounty board.

188. **Merchant Caravan Blessing**
   - Fantasy transport protection with social/religious flavor.

189. **Potion Batch Gone Wrong**
   - Fantasy medical/shop malfunction.

190. **Dragon Tax / Monster Tribute**
   - Fantasy faction/resource pressure event.

### Meta / Systemic Event Ideas

191. **Event Echo**
   - A past event returns in altered form based on player choices.

192. **Chain Reaction Event**
   - One event’s outcome changes the next event spawn table.

193. **Karma Mirror**
   - Similar opportunity appears for a Saint and a Scourge, showing contrast.

194. **Moral Shortcut**
   - Easy selfish solution vs slower generous solution.

195. **Witness Camera Replay**
   - Evidence can be reviewed, stolen, or corrupted.

196. **NPC Memory Payoff**
   - An NPC references an old act and changes the current event.

197. **Faction Ultimatum**
   - A faction demands loyalty after repeated mixed behavior.

198. **Public Leaderboard Pressure**
   - Top Saint/Scourge/Warden gets targeted with special opportunities/threats.

199. **Quiet Good Deed**
   - Helpful action with no immediate reward unless discovered later.

200. **Temptation Event**
   - A high-reward harmful option appears when nobody seems to be watching.

## Player-to-Player Interaction Ideas

These are focused on direct player interactions, especially ones that create karma consequences, social memory, betrayal, cooperation, or emergent stories.

### Cooperation / Help

1. **Rescue Downed Player**
   - Revive or stabilize another player before their downed timer runs out.
   - Karma: helpful, heroic, protective.

2. **Carry / Drag Downed Player**
   - Move a downed player out of danger or toward a clinic.
   - Creates risk: rescuer moves slower or cannot fight well.

3. **Pay Another Player’s Clinic Fee**
   - Cover revive/healing cost for someone who cannot afford it.
   - Strong social goodwill event.

4. **Share Healing Item**
   - Use medicine/repair kit on another player.
   - Can be a quick “good Samaritan” interaction.

5. **Guard While Looting / Repairing**
   - One player performs a vulnerable action while another protects them.

6. **Boost / Assist Interaction**
   - Two players together can open heavy doors, repair faster, lift debris, or move cargo.

7. **Escort Contract**
   - One player hires another for protection during travel/delivery.

8. **Emergency Ping / Call for Help**
   - Player can send a local distress call; others choose whether to respond.

9. **Item Loan**
   - Temporarily lend an item with expected return.
   - Failure to return can become theft/social drama.

10. **Shared Objective Bonus**
   - Players get better rewards for completing rescue/repair/delivery together.

### Trade / Economy

11. **Direct Trade Window**
   - Players exchange items/currency safely.

12. **Gift Item**
   - One-way transfer for reputation/favor.

13. **Sell Item to Player**
   - Player-set prices create an informal economy.

14. **Barter Offer**
   - Trade item-for-item without currency.

15. **Tip / Reward Player**
   - Give scrip after being helped.

16. **Debt / IOU Marker**
   - Lightweight social debt system: “Sean owes Mara 10 scrip.”

17. **Bounty Split**
   - Players who help capture/down a Wanted target split reward.

18. **Contract Board Posted by Player**
   - Player posts job: escort me, retrieve item, guard area, find thief.

19. **Insurance Deal**
   - Pay another player to guarantee return of lost/dropped item.

20. **Item Appraisal**
   - Skilled/trusted player identifies value or contraband risk.

### Conflict / Crime

21. **Pickpocket Attempt**
   - Steal small item/currency if close enough; witnesses matter.

22. **Mugging / Forced Drop**
   - Threaten/attack to force another player to drop cargo.

23. **Challenge to Duel**
   - Formal fight request; accepted duel changes karma rules.

24. **Refuse Duel**
   - Declining is safe, but may create social/reputation flavor depending on context.

25. **Backstab During Alliance**
   - Betray posse/contract partner for strong negative reputation.

26. **Ambush Setup**
   - One player lures another into danger.

27. **Frame Player**
   - Plant contraband or evidence on another player.

28. **Expose Framing**
   - Reveal planted evidence, reversing reputation hit.

29. **Steal From Downed Player**
   - Looting a vulnerable player is allowed but socially/karma costly.

30. **Return Stolen Goods**
   - Return another player’s lost/stolen item for redemption karma.

31. **Ransom Player / Item**
   - Hold stolen item or captured target until paid.

32. **Sabotage Player Structure/Object**
   - Damage something another player placed or owns.

33. **Disarm / Disable**
   - Nonlethal combat interaction that interrupts attack, theft, or escape.

34. **Body Block / Interpose**
   - Stand between attacker and target to protect someone.

35. **Mark as Suspect**
   - Report another player as suspicious; requires evidence/witnesses to avoid abuse.

### Law / Justice / Bounty

36. **Issue Wanted Warrant**
   - Warden marks a qualifying player Wanted.

37. **Citizen Report**
   - Any player can report witnessed theft/violence.

38. **Provide Testimony**
   - Witness player confirms or denies what happened.

39. **Collect Evidence From Scene**
   - Pick up evidence and deliver it to law/faction NPC.

40. **Arrest / Restrain Player**
   - High-trust/lawful player can temporarily restrain a Wanted player.

41. **Break Restraints**
   - Captured player can escape with time, help, or tool.

42. **Bounty Assist Credit**
   - Players who tracked, damaged, revealed, or trapped target get partial credit.

43. **Turn Yourself In**
   - Wanted player can surrender to reduce penalty.

44. **Plead Case to Warden**
   - Accused player can ask for review; social/legal interaction.

45. **Bribe Witness / Warden**
   - Risky corruption interaction; severe if exposed.

### Social / Trust / Reputation

46. **Handshake / Pact**
   - Non-binding social agreement recorded in event log.

47. **Trust Mark**
   - Mark another player as trusted; affects quick interactions/trade warnings.

48. **Warn Others About Player**
   - Social warning creates rumor but may be false/malicious.

49. **Apologize to Player**
   - Formal apology after harm; may reduce social penalty if accepted.

50. **Accept / Reject Apology**
   - Victim decides whether apology resolves the dispute.

51. **Public Praise**
   - Commend another player for good action; helps reputation slightly.

52. **Public Accusation**
   - Accuse another player publicly; evidence determines outcome.

53. **Challenge Reputation Claim**
   - Contest someone’s “hero/outlaw” narrative with proof.

54. **Share Rumor Privately**
   - Whisper a rumor to another player instead of broadcasting it.

55. **Spread Rumor Publicly**
   - Amplify claim about another player, true or false.

56. **Secret Alliance**
   - Two players privately coordinate while appearing neutral.

57. **Reveal Secret Alliance**
   - Expose hidden cooperation or betrayal.

58. **Mentor / Train Player**
   - Experienced player helps another improve or learn mechanic.

59. **Vouch for Player**
   - High-reputation player temporarily lends trust to someone else.

60. **Withdraw Vouch**
   - Publicly revoke trust after betrayal.

### Posse / Group Play

61. **Invite to Posse**
   - Start/join a player group.

62. **Leave Posse**
   - Depart cleanly or during crisis.

63. **Kick From Posse**
   - Group leader removes a member.

64. **Posse Vote**
   - Vote on accepting contract, kicking member, sharing loot, or declaring rivalry.

65. **Shared Loot Rules**
   - Posse chooses split mode: equal, finder’s keepers, leader assigns.

66. **Posse Betrayal Event**
   - Member attacks/steals from posse, generating stronger social memory.

67. **Group Rescue Bonus**
   - Posse rescues someone together for group reputation.

68. **Group Crime Heat**
   - Posse crime spreads lighter heat to all participating members.

69. **Rally Point**
   - Posse can set a temporary meeting point.

70. **Call for Backup**
   - Posse member pings others during danger.

### Communication / Information

71. **Local Chat**
   - Proximity-based communication.

72. **Whisper**
   - Private short-range message.

73. **Shout**
   - Longer-range message that attracts attention.

74. **Posse Chat**
   - Group-only coordination.

75. **Anonymous Note**
   - Leave a note without revealing sender unless investigated.

76. **Signed Note / Contract**
   - Leave proof of agreement or warning.

77. **Map Ping Shared With Player**
   - Point out danger, loot, target, or route.

78. **Share Tracking Clue**
   - Give another player a clue toward Wanted target/stash.

79. **Hide Information**
   - Choose not to reveal a discovered danger/opportunity.

80. **Broadcast Alert**
   - Publicly warn everyone about threat or opportunity.

### Movement / Physical Interaction

81. **Push / Shove**
   - Low-damage displacement; can be playful or criminal based on context.

82. **Block Doorway**
   - Physically prevent movement; may become hostile if abused.

83. **Help Over Obstacle**
   - Boost another player over wall/gap/debris.

84. **Carry Cargo Together**
   - Heavy loot/object requires two players or mount.

85. **Hand Off Cargo**
   - Transfer a carried object quickly.

86. **Mount Passenger Ride**
   - One player drives/rides, another rides passenger.

87. **Kick Passenger Off Mount**
   - Betrayal or safety interaction.

88. **Tow / Repair Mount**
   - Help another player recover vehicle/mount.

89. **Body Shield**
   - Protect downed/injured player by taking hits.

90. **Grab / Pull From Hazard**
   - Pull another player out of fire, vacuum, poison, etc.

### Mischief / Humor / Light Social

91. **Prank Item Use**
   - Whoopie cushion, fake alarm, harmless trap.

92. **Dare Challenge**
   - Player dares another to do a risky/funny action.

93. **Bet on Duel/Race**
   - Players wager on outcomes.

94. **Trade Fake Item**
   - Scam with counterfeit item; reputation risk.

95. **Start Dance/Celebration Emote**
   - Nearby players can join; social glue.

96. **Mock / Taunt**
   - Minor social provocation; may escalate conflict.

97. **Compliment / Cheer**
   - Small positive social action.

98. **Throw Harmless Object**
   - Distraction or joke; can become crime if harmful context.

99. **Rename Posse / Mount Together**
   - Group flavor interaction.

100. **Shared Screenshot / Memory Marker**
   - Players mark a memorable location/event in-world.

## Forced-Interaction Mechanics

The point: a 30-min match needs at least one or two *mandatory* touch-points
that pull players to NPCs, structures, or each other — chores that have a
karma-visible choice baked in. Hunger ("eat once per match") was the seed.
Goal is one decision, not a continuous drain.

- **Hunger / Eating**
  - Categories: `social`, `trade`, `supply`. Theme tags: `theme:any`.
  - Player must eat once mid-match or take a small stat penalty (no health
    loss, just "tired/hungry" debuff).
  - Food can be bought, scavenged, given by another player (positive karma),
    or stolen (negative karma).
  - Hooks the existing `RationPack` and "consumable" tag.

- **Energy Weapon Recharge**
  - Categories: `trade`, `combat`, `supply`. Theme tags: `theme:any`.
  - Energy weapons (electro pistol, plasma cutter) drain a charge meter and
    require a station/vendor visit to top up.
  - Faction-aligned vendors only recharge faction-aligned weapons → uses
    Step 33 store gating.
  - Forces 1–2 mid-match map traversals for ranged-heavy players.

- **Notice / Rumor Board Pickup**
  - Categories: `rumor`, `quest`, `social`. Theme tags: `theme:any`.
  - A physical board structure where players read recent world events as
    text, and quests/rumors only become visible after reading.
  - Eavesdrop drama: someone reads a rumor about *you* nearby.
  - Reuses Step 4 rumor quest module + `WorldEventLog` rendering.

- **Mail / Package Handoff**
  - Categories: `quest`, `social`, `theft`. Theme tags: `theme:any`.
  - Once per match, a courier NPC randomly hands a sealed package to a
    player with a recipient name.
  - Deliver → scrip + karma. Refuse / lose package → courier checks back,
    karma penalty.
  - Package being *contraband* makes great betrayal moments — open and
    inspect, deliver anyway, or rat out the recipient.
  - Reuses delivery quest module + Step 28 contraband flag.

- **Tithe / Tribute Window**
  - Categories: `social`, `karma`, `faction`. Theme tags: `theme:any`.
  - Single mandatory dialog event 5–10 min before match end.
  - Saint can collect community tribute (others give for karma+, refuse for
    karma-).
  - Scourge can demand it (others pay for safety, refuse and risk being
    marked Wanted).
  - Uses Step 22 title broadcast + Step 24 Wanted system.

- **Toll Checkpoint**
  - Categories: `travel`, `trade`, `law`. Theme tags: `theme:any`.
  - Choke-point tiles between regions cost scrip or contraband to pass.
  - Lawless zones (Step 38) become the obvious smuggler bypass route.
  - Cheapest to build — just a "toll" structure category and a
    `ProcessPayToll` intent.

- **Cleanliness / Smell**
  - Categories: `social`, `npc`. Theme tags: `theme:any`.
  - Per-player meter that drifts up (dirty) over match time. Activities
    that boost it: combat, time in lawless zones, sabotage, picking up
    contraband, comedown from drugs.
  - Cleaning options: bathhouse / clinic / wash trough structure use,
    a "Spare Towel" consumable item, or rain tiles (if/when weather
    lands).
  - **NPC interaction modifier**: smell tier shifts vendor prices and
    dialogue tone. Tier band examples:
    - `clean` — small dialogue / price bonus, NPC compliments.
    - `neutral` — default.
    - `smelly` — small price markup, NPC visibly leans away in dialogue
      (vocal mutter cue).
    - `reeking` — NPCs refuse some interactions outright; gated shop
      offers (Step 33) become unavailable until cleaned.
  - **Karma hook**: bathing in a public area (saloon trough) is a
    light-flavored social moment; bathing inside a clinic is the
    "respectful" version. No karma swing for being smelly itself —
    pressure is purely social/economic.
  - Reuses `_persistentStatusByPlayer` (Step 27) for the tier band.

- **Restroom Need**
  - Categories: `social`, `karma`, `npc`. Theme tags: `theme:any`.
  - Per-player meter that fills slowly across the match. At full,
    "uncomfortable" debuff applies (small movement / aim penalty);
    if it stays full long enough, an automatic "accident" event fires
    and applies a hefty Cleanliness penalty.
  - **Restroom structures**: bathroom tiles inside saloons, clinics,
    workshops, and stations. Using one resets the meter cleanly.
  - **Karma-visible choice**: relieving outside a restroom is allowed:
    - In a lawless zone → no penalty (zone overrides karma cost,
      Step 38).
    - In a witnessed area (other players or law NPCs in line of sight)
      → small `chaotic` karma penalty + Wanted-style "Public Indecency"
      flag for a short window (uses Step 24's wanted machinery, lighter
      severity).
    - Out of sight → no karma cost, but Cleanliness ticks up.
  - Holding it too long → automatic "accident" event triggers in the
    open regardless, with the worst-case karma if witnessed.
  - **Social moment**: another player or NPC can offer a "Bathroom
    Pass" / direct you to a nearby restroom for a small karma+.
  - **Cross-mechanic**: combines with Cleanliness — an accident
    immediately bumps Cleanliness to `reeking` until cleaned.

### Design rule for forced interactions

A forced interaction earns its slot only if the *moment of contact* surfaces
a karma-visible choice. "You must eat" is a chore. "Mara is offering you a
free meal in exchange for sharing what you saw at the saloon" is a story.
Bias every entry above toward the second framing.

## Drugs / Substances

Drug system fits cleanly with the contraband (Step 28) + status effects
(Step 27) + faction (Step 33) infrastructure. Tone-fit is critical: this is
a comedy karma sandbox, not a gritty survival sim. Names should read
absurd ("Off-brand Focus Tabs", "Saloon Hangover Cure"), not realistic.

### Effect Pattern

Each drug has three phases:

- **Buff window** (30–60 sec): one stat bumped (attack power, speed, stamina
  cap, accuracy, damage resistance, social karma multiplier).
- **Comedown window** (30–90 sec): same stat flipped negative.
- **Diminishing returns**: each subsequent dose in the same match gives a
  smaller buff and a longer comedown. No cross-match persistence — the
  "addiction loop" stays tractable inside a single 30-min match.

### Karma & Social Hooks

- Most drugs are flagged contraband → existing decay near law NPCs.
- Dealing/buying drugs is shady karma; accepting from a friend is neutral;
  refusing publicly is a small Saint nudge.
- Saints get a one-time "preach sobriety" emote that nudges nearby drug
  users into a forced "use anyway / decline" choice.
- Scourges can run a dealer storefront with marked-up prices.
- Overdose: chaining 3+ doses inside the comedown window triggers a
  short-term "incapacitated" status (similar to Downed) — slapstick rather
  than grim.

### Drug Concept Seeds

- **Combat Stim** — +attack power buff, comedown drops attack to 0; tagged
  `combat`, `contraband`.
- **Wraith Dust** — speed buff (stacks with Wraith perk), comedown leaves
  player slower than walking; tagged `combat`, `contraband`, `chaotic`.
- **Painkiller** — damage resistance buff, comedown halves max HP; tagged
  `medical`, `contraband` (off-brand version), `helpful` (clinic-branded).
- **Focus Tabs** — accuracy / aim-assist buff, comedown gives screen
  jitter / aim wobble; tagged `social`, `contraband`.
- **Euphoric** — comedy buff: social actions give 2× karma in either
  direction. Comedown is just regretful flavor text. Tagged `social`,
  `silly`.
- **Saloon Hangover Cure** — clears a comedown immediately but counts as a
  dose itself (so it just shifts the problem). Tagged `medical`, `silly`.
- **Clinic-Brand Painkiller** — legal painkiller variant sold at clinics
  with shorter buff and milder comedown; uses faction store gating.

### Dealer NPC

A new NPC archetype layered on the existing vendor pattern (Step 33):

- Random roving dealer that appears mid-match in lawless zones / alleys.
- Inventory drawn from a `DrugCatalogue` (separate from
  `StarterShopCatalog`).
- Reputation with the dealer faction grants better prices but worse rep
  with law-aligned factions.
- Trading with a dealer in view of a law NPC triggers an immediate
  contraband-detected event (Step 28).

## Item Brainstorm

Comprehensive item ideas grouped by purpose. Most have a karma-visible
hook so they create story moments rather than just stat changes. All
items should carry theme tags so western/space/post-apoc/fantasy can
re-skin without code changes (see "Theme Variant Crosswalk" at the end
of this section).

### Food & Drink (supports Hunger)

- **Survival Ration** — generic mid-tier hunger fill; non-perishable,
  filling, joyless. Tags: `consumable`, `helpful`.
- **Fancy Meal** — high hunger fill plus small social karma boost when
  shared. Tags: `consumable`, `social`.
- **Spoiled Stew** — looks like a ration but reduces hunger less and
  applies short Poisoned status. Used in karma-negative gifting setups.
  Tags: `consumable`, `deceptive`, `harmful`.
- **Field Snack** — small hunger fill, partial; eaten on the move. Cheap.
  Tags: `consumable`.
- **Communal Pot Stew** — only eaten when standing near a `clinic` or
  `saloon` structure with another player; both get full hunger + small
  karma. Tags: `consumable`, `social`, `cooperative`.
- **Drinking Water** — small hunger contribution but mostly clears
  Poisoned/Burning status. Tags: `consumable`, `medical`.
- **Liquor / Strong Drink** — clears Stamina debuffs but applies short
  Aim Wobble debuff. Tags: `consumable`, `social`, `silly`.
- **Hot Coffee / Stimulant Tea** — short stamina regen boost; legal
  alternative to Combat Stim. Tags: `consumable`, `helpful`.
- **Foraged Berry / Wild Herb** — free if found in lawless zones; small
  hunger fill. Tags: `consumable`, `wild`.
- **Mara's Patch Stew** — clinic-faction-only meal, requires faction
  rep to buy. Big hunger + minor heal. Tags: `consumable`, `medical`,
  `faction:clinic`.

### Ammo & Power (supports gun reload, energy recharge)

- **Ballistic Rounds** — generic projectile ammo for ballistic weapons
  (SMG-11, Shotgun, Rifle-27). Cheap, available everywhere. Tags:
  `ammo`, `ballistic`.
- **Energy Cell** — recharges energy weapons (Electro Pistol, Plasma
  Cutter). More expensive, faction-gated at Civic Repair Guild vendors.
  Tags: `ammo`, `energy`, `faction:guild`.
- **Heavy Slug** — premium round for shotguns / rifles; rare drop, hits
  harder. Tags: `ammo`, `ballistic`, `rare`.
- **EMP Charge** — single-use round that disables vehicles or stuns
  energy weapon users. Tags: `ammo`, `tech`.
- **Quiver of Arrows** — bow ammo for fantasy theme (or "throwing
  knives" for any theme). Tags: `ammo`, `silent`.
- **Empty Magazine** — junk drop from spent reloads. Sells for trivial
  scrip; can be crafted back into ammo with the right ingredients.
  Tags: `crafting`, `junk`.
- **Battery Pack** — placeable structure that slowly recharges nearby
  energy weapons over time. Tags: `placeable`, `tech`, `cooperative`.
- **Solar Cell** — slow free recharge over time, only works in open
  air (not in interiors). Tags: `tech`, `slow`.

### Crafting Ingredients (supports Step 36)

- **Scrap Metal** — universal crafting ingredient; common drop.
- **Wire Bundle** — needed for energy/tech crafts.
- **Circuit Fragment** — uncommon; needed for high-tier tech items.
- **Cloth / Fabric** — apparel and bandage crafts.
- **Medical Herb** — clinic-grown ingredient for Medi Patch crafting.
- **Wild Herb** — found in lawless zones; needed for foraged consumables
  and the off-brand drug recipes.
- **Gunpowder** — ammo crafting input. Carries `contraband` tag in
  faction-controlled regions.
- **Salvaged Frame** — large vehicle/structure repair input.
- **Battery Acid** — niche; needed for certain weapon mods.

### Social / Karma Items

- **Gift Basket** — generic giftable; small karma+ on give, larger if
  given publicly with witnesses. Tags: `social`, `gift`.
- **Apology Letter (sealed)** — written confession that delivers a
  karma+ to the *recipient* and karma+ to *sender* if accepted. Refusal
  is a small social burn. Tags: `social`, `sealed`.
- **Confession Scroll** — public confession; admits a specific
  karma-negative action (server-tracked). Reduces karma penalty for the
  named action by 50%. One-time use. Tags: `social`, `karma`.
- **Token of Thanks** — small ceremonial item players hand to each other
  after a rescue or significant help. Mostly flavor, tiny karma+. Tags:
  `social`, `silly`.
- **Memory Stone / Recording Crystal** — records a player action when
  used near them. Can be presented to NPCs as evidence (resolves
  Wanted, claims bounty), or given as proof in rumor quests. Tags:
  `social`, `evidence`, `tech`.
- **Photograph / Holo-Capture** — quicker version of Memory Stone:
  one-shot photo of the current scene with timestamp + player IDs in
  frame. Used for blackmail or evidence. Tags: `social`, `evidence`.
- **Saint's Blessing Token** — Saints can drop these when issuing a
  blessing (Step 9). Recipients can consume for a one-time karma+ jump
  or a single-target Saint perk effect. Tags: `karma`, `consumable`,
  `saint`.
- **Scourge's Mark** — Scourges can drop these to mark a target. When
  consumed by a player, that player becomes Wanted (Step 24). Tags:
  `karma`, `consumable`, `scourge`, `contraband`.

### Stealth / Subterfuge

- **Lockpick Set** — opens locked structures (interiors, stash boxes,
  shop strongboxes). Use is a `theft`-flagged action; if witnessed, big
  karma penalty. Tags: `tool`, `crime`.
- **Disguise Kit** — temporarily hides Wanted marker and faction
  reputation from NPCs (not players). Wears off after a short window.
  Tags: `tool`, `subterfuge`.
- **Smoke Bomb** — drops a smoke cloud tile that blocks line-of-sight
  for several seconds. Useful for escapes, rescues, ambushes. Tags:
  `tool`, `escape`.
- **Decoy** — placeable that briefly looks and acts like the placer's
  player sprite to NPC AI. Distraction / ambush tool. Tags:
  `placeable`, `subterfuge`, `tech`.
- **Tripwire Trap** — single-use placeable that triggers Downed status
  on whoever crosses it. Owner takes karma penalty when it triggers on
  another player. Tags: `placeable`, `harmful`, `crime`.
- **Bribe Pouch** — give to an NPC for a one-time favor (drop a Wanted,
  unlock a gated shop offer, look the other way during a theft). Tags:
  `social`, `crime`.
- **Forged Document** — alters a faction reputation reading on inspection.
  Detected on a percentage chance; failure marks Wanted. Tags:
  `crime`, `subterfuge`.

### Information / Evidence (supports Notice Board, Mail)

- **Daily Bulletin** — physical paper item generated when a player
  reads the notice board. Carries the latest 3 rumor summaries. Sellable
  to NPCs for tiny scrip; readable by other players. Tags: `info`.
- **Sealed Dispatch** — a courier mail package you cannot open without
  becoming Wanted. Delivery window is tight — late = karma-. Tags:
  `quest`, `sealed`.
- **Coded Cipher** — only the recipient NPC can decode. If intercepted,
  player can sell to the rival faction. Tags: `quest`, `faction`.
- **Wanted Poster (carry)** — a held copy of an active Wanted listing.
  Hand to a Warden NPC to claim partial bounty proxy. Tags: `info`,
  `bounty`.
- **Recording Wire** — passive item that captures nearby chat for the
  next N seconds. Played back to the right NPC creates blackmail
  leverage. Tags: `tech`, `evidence`.

### Mail / Package Variants (supports Mail Handoff)

- **Sealed Package — Standard** — the courier's default.
- **Sealed Package — Heavy** — slows the carrier's movement; bigger
  delivery payout.
- **Sealed Package — Fragile** — taking damage breaks it (karma-).
- **Sealed Package — Contraband** — looks like a standard package; if
  opened or scanned by a law NPC, immediately marks the carrier
  Wanted. Higher payout if delivered without inspection.
- **Sealed Package — Personal Letter** — small payout, but the
  recipient's reaction (read aloud) seeds a rumor across the world
  event log.

### Tribute / Faction Items (supports Tithe Window)

- **Tribute Coin** — special unit of currency for the tithe ritual,
  separate from scrip. Saints accept; Scourges demand. Tags:
  `currency`, `karma`.
- **Faction Pin** — wearable that grants a small reputation buff with
  the matching faction and a small penalty with rivals. Tags:
  `apparel`, `faction`.
- **Pledge Token** — single-use binding item between two players that
  records a promise (deliver X by Y, protect Z, etc.). Server tracks the
  outcome and applies karma when resolved. Tags: `social`, `karma`.

### Toll / Travel (supports Toll Checkpoint)

- **Toll Token** — pre-purchased pass for one checkpoint crossing.
- **Forged Toll Token** — looks legit, fails on inspection. Tags:
  `crime`, `subterfuge`.
- **Caravan Pass** — multi-use travel pass tied to a posse. Tags:
  `posse`, `travel`.
- **Lawless Map Fragment** — reveals the location of one toll-bypass
  route in lawless zones (Step 38). Tags: `info`, `travel`.

### Mounts & Travel Gear

(Mounts already exist via Step 19; these are *consumables* and
*upgrades* that ride alongside.)

- **Saddle Bag** — mount upgrade that increases inventory capacity while
  mounted. Tags: `mount`, `upgrade`.
- **Mount Feed** — restores mount stamina (if/when mount stamina lands).
  Tags: `mount`, `consumable`.
- **Travel Compass** — small persistent buff to map reveal radius
  (Step 39 fog of war). Tags: `info`, `travel`.

### Carry / Capacity (Backpacks)

Implies a per-player inventory cap (server-tracked) that the default
shirt-and-pockets configuration leaves small, with backpack apparel
items extending it. Today inventory is uncapped — adding a cap is the
foundational change this section assumes.

- **Hip Pouch** — small bonus capacity (+2 slots), no movement penalty.
  Tags: `apparel`, `carry`.
- **Rucksack** — medium bonus (+5 slots), small stamina cost on sprint
  start. Tags: `apparel`, `carry`.
- **Cargo Pack** — large bonus (+8 slots), notable sprint stamina drain
  + slightly slower base speed. Tags: `apparel`, `carry`.
- **Smuggler's Vest** — small bonus (+3 slots) with a hidden-pocket
  property: contraband items inside it bypass the law-NPC karma drip
  (Step 28) for a short cooldown after enter-range, but a successful
  contraband detection while wearing it triggers an extra Wanted
  severity bump. Tags: `apparel`, `carry`, `subterfuge`, `contraband`.
- **Posse Cargo Harness** — medium bonus (+4 slots) but only equippable
  while in a posse. Tags: `apparel`, `carry`, `posse`.
- **Pet Carrier** — flavor item that holds one small consumable plus
  one Trinket; mostly silly but has a unique slot type. Tags: `apparel`,
  `carry`, `silly`.

**Mechanics notes:**
- Equipment slot: `EquipmentSlot.Back` (new slot — currently `MainHand`
  and `Torso` exist; `Back` is additive).
- Capacity is enforced server-side on item pickup / loot / craft / buy.
  Over-cap pickups are rejected with "inventory full" prompt.
- Trade-offs encoded as `MovementSpeedModifier` and
  `SprintStaminaModifier` on the apparel record so future apparel can
  reuse the structure.
- Visible on the player sprite — bigger packs are larger overlays, also
  affect minimap dot size (Step 40) so others can read "that player is
  hauling."

**Karma hooks:**
- **Stealing a backpack from a Karma-Broken player** is the same as any
  loot, but its *contents* tick proportionally larger — looting a Cargo
  Pack with 8 items is a louder rumor than looting empty pockets.
- **Smuggler's Vest** caught wearing contraband is a karma+rumor combo
  flag.
- **Posse Cargo Harness** locked to posse membership creates a
  "rage-quit your posse and lose your loot" social moment.

**Theme variants** (extending the existing crosswalk):

| Mechanical | Western | Space | Post-Apoc | Fantasy |
|------------|---------|-------|-----------|---------|
| Hip Pouch | Belt Pouch | Utility Pouch | Scrap Pouch | Coin Purse |
| Rucksack | Trail Pack | Crew Backpack | Salvage Sack | Adventurer's Pack |
| Cargo Pack | Mule Pack | Cargo Rig | Hauler Rig | Pack Mule Harness |
| Smuggler's Vest | Hidden-Pocket Duster | Flight Jacket | Patched Vest | Cloak with False Pockets |
| Posse Cargo Harness | Posse Saddlebag | Squad Rig | Convoy Harness | Guild Pack |
| Pet Carrier | Critter Crate | Companion Pod | Mutant Box | Familiar Satchel |

### Junk / Trade Filler

The shop economy needs items that exist mostly to be traded, gifted,
mocked, or sold cheap. The current `WhoopieCushion` and `DeflatedBalloon`
are the lineage. More in that vein:

- **Scrap Trinket** — useless junk; can be gifted as a passive-aggressive
  insult or sold for 1 scrip.
- **Lucky Charm** — flavor only; small persistent karma flavor text.
- **Faded Photograph** — picture of someone you don't recognize.
  Occasionally an NPC reacts strongly when shown.
- **Broken Music Box** — plays a glitchy 2-second tune when used. Pure
  flavor.
- **Found Letter** — fragment of someone else's correspondence.
  Occasionally connects to an active rumor quest.

### Theme Variant Crosswalk

Same mechanical item, different theme flavor. When seeding a world by
theme, the catalog filters or renames items but the underlying record
shape doesn't change.

| Mechanical | Western | Space | Post-Apoc | Fantasy |
|------------|---------|-------|-----------|---------|
| Ballistic Rounds | Bullets | Kinetic Slugs | Hand-loaded Rounds | Crossbow Bolts |
| Energy Cell | Battery Pack | Plasma Cell | Salvaged Capacitor | Mana Crystal |
| Survival Ration | Trail Jerky | Hydroponic Bar | Canned Mystery | Hardtack |
| Fancy Meal | Saloon Steak | Officer's Mess | Pre-War Feast | Banquet Roast |
| Drinking Water | Canteen | Recycled H₂O | Filtered Sludge | Waterskin |
| Liquor | Whiskey | Ration Booze | Distilled Fuel | Mead |
| Stim | Coffee | Wakeful Tab | Caffeine Pill | Energizing Tonic |
| Disguise Kit | Duster + Hat | Spacer Coveralls | Raider Mask | Cloak of Passing |
| Lockpick Set | Skeleton Key | Multi-Tool Slate | Pry Bar | Thieves' Picks |
| Smoke Bomb | Dust-Up | Smoke Canister | Tire Fire Bomb | Smoke Powder |
| Decoy | Stuffed Effigy | Holo-Drone | Mannequin | Mirror Image Charm |
| Bribe Pouch | Bag of Coins | Credstick | Scrip Bundle | Gold Pouch |
| Memory Stone | Tin-type Plate | Holo-Recorder | Photo Camera | Memory Crystal |
| Sealed Dispatch | Express Letter | Encrypted Datapad | Sealed Tape | Wax-Sealed Scroll |
| Tribute Coin | Silver Eagle | Federation Mark | Bottle Cap | Crown Stamp |
| Toll Token | Stage Pass | Transit Chit | Gate Slip | Bridge Marker |
| Lucky Charm | Rabbit's Foot | Captain's Coin | Bottle Cap | Worn Talisman |
| Saint's Blessing | Preacher's Coin | Communion Wafer | Faded Medal | Holy Sigil |
| Scourge's Mark | Black Spot | Outlaw Beacon | Raider Brand | Cursed Sigil |

### Item Tag Inventory (proposed)

When implementing, lean on tags rather than per-item if/else. Suggested
tag dictionary for the new items:

- **Function**: `consumable`, `placeable`, `tool`, `apparel`, `mount`,
  `ammo`, `currency`, `evidence`, `info`, `quest`.
- **Theme intent**: `theme:any`, `theme:western`, `theme:space`,
  `theme:post-apoc`, `theme:fantasy`.
- **Karma valence**: `helpful`, `harmful`, `cooperative`, `social`,
  `silly`, `subterfuge`, `crime`.
- **Faction**: `faction:clinic`, `faction:guild`, `faction:settlers`,
  `faction:dealers`, `faction:law`.
- **Resource type**: `ballistic`, `energy`, `wild`, `crafting`, `junk`.
- **Status**: `contraband`, `sealed`, `rare`, `legendary`, `gift`.

## Absurd Interactions

The point: the karma sandbox needs both **extreme drops** (rare, story-
making, designed to be regretted) and **silly social glue** (frequent,
low-stakes, designed to be remembered). The middle ground is the
sweet spot — moments that are dark *and* funny at the same time.

### Tone Calibration

Lean **Rimworld-tasteful** rather than gritty realism. Frontier comedy
allows desecration, betrayal, and grim outcomes as long as they're
absurd in framing — a fancy meal stolen from a corpse, a public funeral
held for someone who isn't dead, a posse leader betraying their crew
for a sandwich. Avoid horror-genre framing (no body horror, no torture
spectacle, no anything that doesn't survive a comedy-club retelling).

Hard tone rules:

- No real-world atrocity flavor. The game has Karma Breaks, not
  executions.
- No NPC abuse that targets vulnerable archetypes (children, NPCs
  presented as helpless without comedic context).
- "Dark" actions should always have a karma-visible cost commensurate
  to the action. A player who chooses extreme actions should expect to
  hit Abyssal tier fast and feel the social weight.

### Extreme Karma Drops (rare, story-making)

These are intentional cliff actions — a single use should drop a
neutral player into Outlaw or Wraith tier, and a Scourge contender
into Abyssal tier. Designed to be regretted and remembered.

- **Loot a downed player mid-rescue** — robbing the body while another
  player is actively carrying / reviving them. Big karma- + 90s Wanted
  flag for the looter; rumor with the rescuer's name attached.
- **Kill a rescuer carrying a downed player** — taking out a player
  mid-rescue with the carried body still on their shoulder. Triggers
  a *witnessed massacre* event if any NPC is in line of sight.
- **Burn down a clinic / saloon / claimed station** — destroy a
  community structure (interior tiles all flagged `compromised`,
  permanent for the match). Massive karma-, locks the player out of
  the affected faction's vendors entirely.
- **Poison the saloon punch bowl** — drugs the entire saloon's local
  chat radius with a comedown debuff. Karma- per affected player +
  rumor. Dark-comedic; reuses Step 28 contraband + drug system.
- **Wear a dead player's Dog Tag in their face** — equip a trophy
  (Step 35) while standing within local chat range of the victim's
  next respawn. Triggers "publicly mocked" event, big karma- for the
  wearer, sympathy karma+ for the victim.
- **Sell a posse member to a bounty hunter** — leak the posse member's
  position to a third player who has them as a Wanted target. Big
  karma- for the betrayer, immediate posse expulsion, 5-min reputation
  flag with all factions.
- **Bribe a clinic to refuse a revive** — pay a clinic NPC to ignore a
  specific player's auto-revive (Step 16) for the rest of the match.
  Karma- + clinic-faction reputation crash.
- **Salt the supply drop** — pre-drug a supply drop crate before
  another player claims it (Step 30). Whoever claims it inherits the
  comedown. Karma- for the salter; the victim gets a free rumor.
- **Forge a confession in someone else's name** — submit a false
  Confession Scroll claiming the *recipient* committed an act they
  didn't. NPCs treat the named player as the offender for a window.
  Big karma- if the truth surfaces.
- **Loot a downed Saint** — robbing a player while they're holding the
  Saint title is its own multiplier — karma- is doubled.
- **Hold a fake funeral for a living player** — invite witnesses, give
  a eulogy in local chat, mock the "deceased." If the named player
  arrives during the funeral, big karma- for the host + rumor. If the
  named player joins in willingly, both get a karma+ for the silly.

### Comedic Interactions (frequent, low-stakes)

Designed to be reused often. Mostly silly karma+/- in small amounts;
the value is the *story they create*, not the score change.

- **Slap with a fish** — single-use consumable interaction; recipient
  gets a Smelly tier bump. Both players get a tiny karma+ if accepted
  in good humor. Tags: `silly`, `social`.
- **Mock funeral procession** — posse-only emote that has all members
  walk single-file with a sad music cue. Pure flavor.
- **Statue mode** — emote that locks the player in a pose; nearby
  players can react ("admire", "mock", "pose with"). After 2 minutes
  in pose, NPCs mistake the player for actual statuary and do not
  greet.
- **One-boot heist** — steal exactly *one* equippable shoe from a
  sleeping or downed player. Smaller karma- than a full loot, but
  the victim now walks with a limping stamina debuff and a visible
  one-boot sprite until they replace it.
- **Group chicken dance** — posse emote where all members synchronize
  the dance. While dancing, all members get a small Cleanliness reset
  (working up a sweat is canon).
- **Snowman around sleeping player** — placeable interaction that
  builds a snow/dirt mound around a stationary player. They wake up
  encased; takes 5 sec of struggling to break out. No karma change;
  pure comedy.
- **Yell FIRE in the saloon** — local chat with the literal text
  `FIRE!` in a `saloon` interior triggers an NPC panic event (NPCs
  flee outside for 30 sec, prices rise temporarily). Tiny karma- if
  there's no actual fire. If there *is* an actual fire (post-arson),
  this resolves to a *good* karma flag.
- **Order a fancy meal and walk out** — buy a Fancy Meal at a saloon
  vendor, leave it on the counter without eating it. The vendor
  silently flags you for a small price markup next purchase. Funny
  passive-aggression.
- **Public proposal** — offer a Pledge Token to a player publicly
  with a "marriage proposal" flavor. Acceptance = silly social karma+;
  refusal in front of witnesses = mild humiliation karma- for the
  proposer. Either way: rumor.
- **Apology spam** — give the same player 5 Apology Flowers in 60 sec.
  Triggers an "excessive apology" silly karma- for both players (the
  apologizer for being weird, the target for tolerating it). Rumor.
- **Talk to a corpse** — emote that "starts a conversation" with a
  dead/downed player. NPCs in line of sight react with the muttered
  "uneasy" cue (see SOUND_NEEDED.md NPC vocals). No karma change.
- **Pet carrier sneak-up** — give a Pet Carrier with a Whoopie Cushion
  inside as a "gift." Recipient opens it, deflation prank fires.
  Mutual silly karma+ if both laugh (server can't tell — random
  small karma swing both ways).
- **Trade a single coin in dramatic silence** — engage a player in
  trade, transfer 1 scrip, walk away without saying anything. Triggers
  a flavor rumor logged as `mysterious_one_coin_exchange`.
- **Smelliness contest** — three or more players in a saloon all
  voluntarily flag their Cleanliness; the worst tier wins and gets a
  silly title for the rest of the match.
- **NPC cosplay** — equip clothing matching an existing NPC profile
  exactly, stand near that NPC. NPC has a confused dialogue variant.
  Light social karma+ if the NPC is amused; karma- if law NPC sees
  it as impersonation.

### Dark-Comedic Sweet Spot

The actions that are *dark and funny at the same time*. These are the
ideal — they create stories players retell. Lean into these when
designing new interactions.

- **Inherit a stranger's mail** — player dies / Karma Breaks while
  carrying a Sealed Dispatch (mail handoff system). Whoever loots
  becomes responsible for delivery. The original recipient has no
  idea. Comedy emerges from delivery awkwardness ("uh, sorry, the
  original courier is dead, here's your love letter").
- **Bathroom Wanted speedrun** — player relieves themselves outside,
  is witnessed, gets the "Public Indecency" flag, *then* gets ambushed
  by a Warden mid-zip-up. Pure dark slapstick.
- **Possessed by your loot** — wearing a victim's full kit (their
  weapon, hat, dog tag) gives a temporary "imitating the deceased"
  flavor: NPCs greet the wearer by the dead player's name. Karma-
  for the wearer if the actual victim sees it on respawn.
- **Drug-fueled clinic invasion** — a Combat Stim'd-up player charges
  into a clinic, demands free healing for a downed friend; clinic
  refuses; they shoot a clinic NPC (huge karma-); friend gets revived
  by a *passing rival* in the chaos who claims karma+. Multi-stage
  karma cascade.
- **Communal pot betrayal** — a player invites another to share
  Communal Pot Stew. The pot is poisoned. The host loses karma the
  moment the guest's Poisoned status applies. Comedy = the host has
  to *also* eat from the same pot to sell the trick (server can detect
  the host skipping it as suspicious).
- **Posse cargo harness rage-quit** — player leaves the posse
  voluntarily, instantly losing the slot bonus → over-cap items drop
  on the spot. Posse members can scoop them up. Karma-neutral for the
  quitter, but rumor flavor records the moment.
- **Saint-vs-Scourge accidental same-meal** — Saint and Scourge,
  unaware, both reach for the same Fancy Meal at a saloon. Server
  resolves with a "shared meal" event: both forfeit their title for
  60 sec while the public absurdly assumes they're conspiring. Rumor
  + temporary title swap.

### Implementation Notes

- All "extreme" actions go through the existing `KarmaAction` catalog
  with named tags (`extreme`, `betrayal`, `desecration`) so balancing
  the karma cliffs is centralized.
- Comedic actions reuse the "silly" tag and produce small swings; the
  value is in the rumor + sound + animation, not the score.
- Most dark-comedic moments are *emergent* — they happen because two
  or more existing systems collide. Don't hard-code them; instead,
  make sure the underlying systems (mail, drugs, mounts, status
  effects, witnesses) compose cleanly.
- "Witnessed by NPC or player" is the karma multiplier for almost all
  of these. The witnesses system from BRAINSTORMING.md "Public
  Witnesses" is the prerequisite for the dark-funny axis to land.
