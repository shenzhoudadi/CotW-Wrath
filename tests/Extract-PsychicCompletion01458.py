import hashlib,json,pathlib,re,sys,zipfile

root=pathlib.Path(__file__).resolve().parents[3]

source=root/'outputs/CallOfTheWild.Wrath.Prototype/src'

chosen_cleric='67819271767a9dd4fbfd4ae700befea0'

batch01416={'0f8939ae6f220984e8fb568abbdfba95','7bc8e27cba24f0e43ae64ed201ad5785','0c17102f650d9044290922b0fad9132f','87ec6541cddfa394ab540dd13399d319'}

required=set(["14a1fc1356df9f146900e1e42142fc9d","4cd1757a0eea7694ba5c933729a53920","90e54424d682d104ab36436bd527af09","1e1f627d26ad36f43bbd26cc2bf8ac7e","efc60c91b8e64f244b95c66b270dbd7c","c714cd636700ac24a91ca3df43326b00","11f971b6453f74d4594c538e3c88d499","97e216dbb46ae3c4faef90cf6bbe6fd5","9972f33f977fc724c838e59641b2fca5","0f15c6f70d8fb2b49aa6cc24239cc5fa","4c44724ffa8844f4d9bedb5bb27d144a","6ffd93355fb3bcf4592a5d976b1d32a9","e81cd772a7311554090e413ea28ceea1","5898bcf75a0942449a5dc16adc97b279"])

required.update(['1af015a346ef453d8f0151343c27efd9', '2b35e887d852cb949a1d8e308efe8065', 'ff1cf8952b8551f4c9fbe32966f9a945', '8bc94d17348204041ab8863d4f841a99', '5e02868509087b248a2c6daad16e9aef', '137c8f8182413dc4e80abe2a9d0313b5', '6cd07c80aabf2b248a11921090de9c17', '9802c5950219c6c41a9293076e221d12', '66a62322b44f25c418472df9a77f5b3a', 'e66d3b4d6669f9f4ea79056d4a525098', '3e427e1fb78631c4b9f17e39bc19c96f', 'c2fa8c5d307efbb41a15d55373f414e2', 'c91f9e996c0af364dae41569cb9586e7', '313c37a6410ac514b835490cf768794f', '3d4c6361e60fa664db01d5709baaa812', '8b9ffc4cd53b82f44ab7c0ddca85877d', 'e3eddc31b2430b7408c62f313efea4bc', 'd7767bd68ca218c4cb99e35b1cc50d8c', '07e1c038726e0824eabaa39e6038a854', 'e7be24dd1cf591d4baecd9578705513d', '1c06611ab264115479d16c0ff3ab0446', '29b3a96956eaf8d4cb42205ee233a158', '8e7f48a657b6380429dc1fc685096288', '98293e0bfb86f4f418dcfbe7bf7f8fc2', '1d33fcbc7f5175446bb9577e0f7811b3', 'a824b1806059bd54db0b5b0093472f21', '487981678b77e0b4aa5ff0328f50ecb2', 'ba28f4a77f3741c4187b283d42e22554', 'e80c565458d9c0c4ea1323dc8cf122d9', '4de66387a2c36624385930b0f5e596af', 'dcabd85e723bac941bc5807a6a953e89', 'ff2f3177d34778b41b5bfe0b140cba42', '5617c38bde7d56f4094b2fc714399566', '3e14f46a35228b946af0c1fe79870e54', '1b1e0ae5916e6564ba3f0e2ab4fc403a', '75edd403e824aa048ab5d4827b803b08', '48f55c7834a2fe845928337f091a0932', 'c9097a1d0685f2f468ab63c1138815ad', 'b1505c6512a0f2c4581dfa6af14e9d5c', '43738e1ea3c328549bcdd81883841e05', 'e98cf086294d6134bb76d35e090cb059', '4bd01292a9bc4304f861a6a07f03b855', 'd25c72a92dd8d38449a6a371ef36413e', '899b08dfc31868e4cb2c6287df9d355c', '954650e8a7542e642819716bb78bee86', 'ba52aed3017521a4abafcbae4ee06d10', 'eaf7077a8ff35644883df6d4f7b2084c', 'ed4fbfcdb0f5dcb41b76d27ed00701af', '46cf5c995494e784c8d9a1696f9c61a7', 'e7ecd11651b4df34897f33271a8d1cfc', '630ea63a63457ff4f9de059c578c930a', '8f0eb58c2d6aeab4e8523ec85b4b2bc7', '5adb5a0650f5b2049bab1afe822bd3bd', '4a511e1dfeec46c4b867bf61b15eae2b', 'd560ab2a1b0613649833a0d92d6cfc6b', '1bb5466b9bfcb5e47b9f667dad5784f9', 'cedc4959ab311d548881844eecddf57a', '0d38e470e350ce34f869c20002d45763', '3cae80cecdc2fb84b86587032132d48f', 'b5816eb8a1c295b42b1edb0b9f48dfd8', 'fcaaf1f25440a8f40ae424c7708e9a0f', '3108e18055b46b84f8a57898ad5ad075', 'e7489733ac7ccca40917d9364b406adb', '7244a24f0c186ce4b8a89fd26feded50', '5fa471f18a914f743b1200b7520c215d', 'e946e3fdc42ea65408daee0f029b4100', '0e8f3d24a2cea644e91273f6c551152d', '05bba992bea833349a246fd47042748c', '1411baa7c0e58f547b76b6b13e5443f3', '3408c351753aa9049af25af31ebef624', '40d201c6fbbb46e46a63dec8508de65a', 'b5c4c51cc9993b14b9dc0f0c54199d09', '0a6effb356101cc46aa0bed8c3ab6fd4', 'a69fb167bb41c6f45a19c81ed4e3c0d9', 'd77e67a814d686842802c9cfd8ef8499', '145f1d3d360a7ad48bd95d392c81b38e', '1b9873f1e7bfe5449bc84d03e9c8e3cc'])

required.update(['3c380607706f209499d951b29d3c44f3', '48ac8db94d5de7645906c7d0ad3bcfbd', 'b8cecf4e5e464ad41b79d5b42b76b399', '45a4607686d96a1498891b3286121780', 'e9dc4dfc73eaaf94aae27e0ed6cc9ada', 'd4b54d9db4932454ab2899f931c2042c', '363cab72f77c47745bf3a8807074d183', '5f3126d4120b2b244a95cb2ec23d69fb', '6507d2da389ed55448e0e1e5b871c013', 'f74c6bdf5c5f5374fb9302ecdc1f7d64', '3a8d34905eae4a74892aae37df3352b9', '287682389d2011b41b5a65195d9cbc84', '88da2a5dfc505054f933bb81014e864f', '40437224e8606d644a1986311981c1eb', '318ae316d7aed5143b7a8cbd25fd8e77', '0ad7c2825c8504642b571265757d7037', '2f1619e253ea6a04087def71c7925715', '8140fe21fd91da3439bf5ed017ca9f90', '87d6de4d30adc7244b7a3427d041dcaa', '1054ea2fa21176249b96a63d54f02b17', '1b18d6a1297950f4bba9d121cfc735e9', 'e0401ecade57d4144978dbd714c4069f', 'bd75a95b36a3cd8459513ee1932c8c22', '9cdfc2a236ee6d349ad6d8a2170477d5', 'a7a7ad500d4e2a847b450b85cbe68d65', '8bb8579622b823c4285d851274a009c3', '2a0ce0186af38ed419f47fce16f93c2a', '4923409590bdb604590e04da4253ab78', 'baebdad7bb0a44a4bba402859d0cedb1', '3ab76d4a8aa9e4c459add32139080206', 'c062c6d16aecddc4ab67d9c783b2ad46', 'd5c04077fc063e44784384a00377b7cf', '0bbf10151dd1d8d4c8653d245e425453', '1cb0b559ca2e31e4d9dc65de012fa82f', '791d888c3f87da042a0a4d0f5c43641c', '1bbca102706408b4cb97281c984be5d5', 'f111037444d5b6243bbbeb7fc9056ed3', '7ba93e2b888a3bd4ba5795ae001049f8', '97dff21a036e80948b07097ad3df2b30', '952f342f26e2a27468a7826da426f3e7', '61aeb92c176193e48b0c9c50294ab290', '5551dd90b1480e84a9caf4c5fd5adf65', 'adf124729a6e01f4aaf746abbed9901d', '4d48365690ea9a746a74d19c31562788', '689b16790354c4c4c9b0f671f68d85fc', '3c0b706c526e0654b8af90ded235a089', '7d2233c3b7a0b984ba058a83b736e6ac', '4c936de4249b61e419a3fb775b9f2581'])

required.update(['557cd05f067b44e8a916d6324c0c6c42', 'a995ae02b3e741439b82e5e5d5a2e046', '26e81d83d13dbf6448b10019c802612d', '92918c8de8f0c7b448531cf3a4fbb6ad', 'e788846aaf449404db11e51182174be8', '65b5e7f17a1b928418fc9e8a6b55eafa', '004eb31f1217d444b91a5404f3c0a385', '24dc5eb552a9e2c4cb9a17c355a80d2e', '50efeb23b314869409365c8061a4082d', 'd9fbc976432e4494788f5d0996e8a52f', '659fbc54fc519b44dacacc78e7d46dec', '44c130a12d49ebf4a95945feffe5aae7', 'e9126b55e8734004cbbe8bb5fbb9aedd', 'bbae401660bbad94c865d71029d8439e', 'f9ee5a9146561ba4bac4cb8a7126f85c', 'de6758cb6044b3f498ac0fa2b3e440d3', '1e6ea0d1d642c8c43ab7e72dd8f607a9', '379d33ef16e812146b5517601ab25e66', 'f3e19b0d6d82e2a4d98957af591f5d36', '810e2a7009ed3a84c81b398b2763e7a8', 'acff6b0cf279a31439010afea01df912', '9be79014f07a3fb4f90ad48e19e23e41', '87a3e296757412e45910493e5fed1417', '2196e093593903f4ebc36467ccbf1d9a', 'bfbed44eafaa4344189730403b426a88', '190ebde2d9d80c94783bcb73b9106d99', 'eae7cee2a5da93442a7563dfeda33432', '11a92d0a44576e4439b745ab00f298b5', '0bf6f90fdcb864b4486344100391b478'])

required.update(["f1a70d9e1b0b41e49874e1fa9052a1ce","34ecd1b5e1b90b9498795791b0855239","26b10d4340839004f960f9816f6109fe","339659f6b09b4136bf8c2b2c3df87c65"])

required.update(['a8e7e315b5a241b47ad526771eee19b7', 'b1b7eeb49d703b047a3a534f17863001', 'fc246eb29c8f95c449fd156759f421e9', '4d2930114daad304da816b1412c85d1d', '99345ce7c0f8001438fee23db660d039', 'fbc7fe386bceb354494b05736381c238', 'e73da542e92df1843a93e29701a2e1d8', '00369aa0a76141a479382e360b1f3dd7', '61b9776ff6270ac48b0daffec5032052', 'fd66fc50ec2a65347a8fa3352aa3c581', '456a099d26ef1e84c9401666870de095', 'f6ac26437d007544299bd113d58a3858', '4594286e4a565cd41ad9475f6c903fda', '2e3f01df36b508b4e9186bab7a337dfa', '481b03bc6cbc5af448b1f6cb70d88859', 'db0f61cd985ca09498cafde9a4b27a16', '886c7407dc629dc499b9f1465ff382df', '9c70d2ae017665b4b845e6c299cb7439', 'a4fc1c0798359974e99e1d790935501d', '0c0afabcfddeecc40a1545a282f2bec8', '9b93040dad242eb43ac7de6bb6547030', '61bc14eca5f8c1040900215000cfc218', '61fb13235c614f744ad42ff6141fab0e', '75254f19ca6e1d048a88b7545bb65221', 'c33f2d68d93ceee488aa4004347dffca', '20b57bab6bac9b04493491432bcb6868', 'bd09b025ee2a82f46afab922c4decca9', 'c1b26f97b974aec469613f968439e7bb', '37a5e51e9e3a23049a77ba70b4e7b2d2', '13c87ac5985cc85498ef9d1ac8b78923'])

required.update(["47808d23c67033d4bbab86a1070fd62f","e5cb4c4459e437e49a4cd73fde6b9063","f5fc9a1a2a3c1a946a31b320d1dd31b2"])

required.update(['1f8a2e1e5e078014baebc90c2c46796f', '340ab07dff974a841b76f22ba4c3cf84', '767e6932882a99c4b8ca95c88d823137', 'a000716f88c969c499a535dadcf09286', 'd2f99947db522e24293a7ec4eded453f', 'de21b6c00e6adaa409a6e7c2ae9f87f4'])

required.update(["25ec6cb6ab1845c48a95f9c20b034220","f08a7239aa961f34c8301518e71d4cdf","ac8d4d2e375a8c841a19ed46696e5af2","059c9f3bba1a42e8a1a898987522dae6"])

required.update(["4ac47ddb9fa1eaf43a1b6809980cfbd2","0a2f7c6aa81bc6548ac7780d8b70bcbc"])

required.update(["c78506dd0e14f7c45a599990e4e65038","83346ff4ee22aea448368e06d2dc4c39","6f408add25eb4c6ca4d40d7e9f809d62"])

required.update(['36897b36a4bd63146b1d8509a17fc5ad','4baf4109145de4345861fe0f2209d903','41bab342089c0254ca222eb918e98cd4'])

# Regression cases from the 0.9.3 tester report. Extract from the installed

# game instead of assuming flags/components on its debuffs and learned spells.

required.update(['bd81a3931aa285a4f9844585b5d97e51','c7104f7526c4c524f91474614054547e',

 '7658b74f626c56a49939d9c20580885e','d2aeac47450c76347aebbc02e4f463e0',

 '886c7407dc629dc499b9f1465ff382df','11cb2fe4fe9c44b448cfe1788ae1ab59',

 '2cfcce5b62d3e6d4082ec31b58468cc8','8b3b4c225fe0fb046bfa8881c3ddad0d',

 '444eed6e26f773a40ab6e4d160c67faa','41e8a952da7a5c247b3ec1c2dbb73018',

 '55a037e514c0ee14a8e3ed14b47061de','c5c86809a1c834e42a2eb33133e90a28'])

required.update(['e19484252c2f80e4a9439b3681b20f00','3aa4cbdd4af5ba54888b0dc7f07f80c4','70a139f0a4c6c534eaa34feea0d08622','ee9ab0117ca06b84f9c66469f4428c61','0b55d725ded1ae549bb858fba1d84114'])

required.update(['dd17090d14958ef48ba601688b611970', 'ef1cd58e0b7fc7f45baedb09407a1cd1', '79c6421dbdb028c4fa0c31b8eea95f16', 'da03141df23f3fe45b0c7c323a8e5a0e', '41c8486641f7d6d4283ca9dae4147a9f', '90f105c8e31a6224ea319e6a810e4af8', '0ce1432869c74b778ab48890edf2021d', '66befe7b24c42dd458952e3c47c93563', '8e627812dc034b9db12fa396fdc9ec75', 'db3c26e791cb4b70a82203b28563da02', 'c5158a6622d0b694a99efb1d0025d2c1', 'f1add10c87fa4563ad5f71779eecde19', '78fffe8e5d5bc574a9fd5efbbb364a03', '303fd456ddb14437946e344bad9a893b'])

foundation=(source/'PsychicClass.cs').read_text(encoding='utf-8-sig')

required.update(batch01416)

required.update({'d99dfc9238a8a6646b32be09057c1729','76437492f801f054ba536473ad2fde79','45102fd7aab96f94d81ec80768549e12','bd63d3ea0e030cf4c930db9ba5eaf931'}) # Native totem-family reciprocity contracts.

required.update({'8c01e7fccbb829947bc5894f63fb389a','952e30e6cb40b454789a9db6e5f6dd09'}) # Actual native secondary bite and its weapon type.

required.update({'a8957a1c6f212244d969645bc2fa7c25','0eaf9696c9eb46f49898b6a5938bd2fb','7c3e509fae07eaa458eae0d6860b922f','e63719ab6f539ab4bb6aaae135f30736','d673c30720e8e7c4bb0903dc3c9ab649','9a2f0ffe517d221459640a4ad85710d7'}) # Exact Daemon Totem constant rider, three tier effects and two caster gates.

required.add('7517c892b30fdf643b33807a2f5abd31') # Actual Guarded Stance activatable leaf for the constant-power negative case.

required.update({'ba1ae42c58e228c4da28328ea6b4ae34','71fe5a730db173243bda4901bc074780'}) # Two native poison sources for 01428 partial/full dispel contracts.

required.update({'c610310d31414edabcedf0c8a6fe32c4','1bb6f0b196aa457ba80bdb312dc64952','32429740d6a5470aaaa02f20d61e43d3','8a63b06d20838954e97eb444f805ec89','8c3102c2ff3b69444b139a98521a4899','e8445256abbdc45488c2d90373f7dae8','e69a85f633ae8ca4398abeb6fa11b1fe'})

for field in ('RootId','WizardId','SorcererBookId','ProficienciesId','ExtendFeatId'):

    required.add(re.search(r'const string '+field+r' = "([0-9a-f]{32})"',foundation)[1])

for name in ('PsychicBoundWeapons.cs','PsychicStorageSpells.cs','PsychicSpiritWeapons.cs','PsychicLinkedWard.cs','PsychicEidolonHealing.cs','PsychicFireSeeds.cs','PsychicFieryShuriken.cs','PsychicVoidOrb.cs','PsychicHexVulnerability.cs','PsychicSpells.cs','PsychicDisciplines.cs','PsychicPresentation.cs','PsychicBurstSpells.cs','PsychicFurtherOptions.cs','PsychicUtilitySpells.cs','PsychicRestorativeSpells.cs','PsychicAfflictionSpells.cs','PsychicForceSpells.cs','PsychicCrushSpells.cs','PsychicPageantry.cs','PsychicSilence.cs','PsychicInsightSpells.cs','PsychicComfortSpells.cs','PsychicUndeadSpells.cs','PsychicWardingSpells.cs','PsychicBloodSpells.cs','PsychicGlobeSpells.cs','PsychicCompulsionSpells.cs','PsychicFluidForm.cs','PsychicChannelVigor.cs','PsychicBladeTutor.cs','PsychicSecondWind.cs','PsychicGhostbaneDirge.cs','PsychicBloodyClaws.cs','PsychicBattleSpells.cs','PsychicWeaponSpells.cs','PsychicTacticalSpells.cs','PsychicAreaSpells.cs','PsychicAspectSpells.cs','PsychicWeatherSpells.cs','PsychicCurseSpells.cs','PsychicSupportSpells.cs','PsychicTravelSpells.cs','PsychicOrbSpells.cs','PsychicCataclysmSpells.cs','PsychicShapeSpells.cs','PsychicConduitSpells.cs','PsychicMagicalShape.cs','PsychicSpiritSpells.cs','PsychicTemporalStasis.cs'):

    required.update(re.findall(r'Native<[^>]+>\("([0-9a-f]{32})"\)',(source/name).read_text(encoding='utf-8-sig')))

required.update(['6afa347d804838b48bda16acb0573dc0','75b3978757908d24aaaecaf2dc209b89','ee51f69e69cb43c3bc7c02002e482d17','aad496a7c9bee224cbd01a3cf8e42061','5a25185dbf75a954580a1248dc694cfc'])

for name in ('PsychicRestorativeSpells.cs','PsychicCommonSpellLists.cs'):

    required.update(re.findall(r'"([0-9a-f]{32})"',(source/name).read_text(encoding='utf-8-sig')))

faith=re.sub(r'AssetId\s*=\s*"[0-9a-f]{32}"','',(source/'PsychicFaith.cs').read_text(encoding='utf-8-sig'))

required.update(re.findall(r'"([0-9a-f]{32})"',faith))

link_ids=set(re.findall(r'new PsychicSpellLink\("([0-9a-f]{32})", "[^"]+", "Wrath"',(source/'PsychicSharedSpellCatalog.cs').read_text(encoding='utf-8')))

required.update(['f4201c85a991369408740c6888362e20','8bc0190a4ec04bd489eec290aeaa6d07','56f94badbba018b4b8277ce6e2e79e72','e9298851786c5334dba1398e9635a83d'])

required.update(['f7d7eb166b3dd594fb330d085df41853','4d588290ed0736e4f8cd3f4c3beacd69','da8ce41ac3cd74742b80984ccc3c9613','3513326cd64f475781799685c57fa452','ecc22ca1eea1bf6488a0d7c6ee2527d8','2476514e31791394fa140f1a07941c96'])

for name in ('Inquisitions01426.cs','TerrifyingHowl.cs','MonkKiPowers01427.cs','Inquisitions01428.cs','PurifyingFlames01428.cs','WizardSubschools01428.cs','EnhancementSchool01429.cs','FlameJet01429.cs','AngerInquisition01431.cs','FerociousBeast01431.cs','WizardSubschools01431.cs','KineticUtility01433.cs','SpiritualGuardian01433.cs','KineticInvocation01433.cs','RagePowerMigration01434.cs','SuperstitionRage01434.cs','AntimagicRage01434.cs','WizardDiscoveries01435.cs','BardMasterpieces01435.cs','TimeStutter01437.cs','Invoker01437.cs','WarDrummer01441.cs','Sunsinger01443.cs','UrbanSkald01443.cs','RogueSwashbuckler01443.cs','HedgeWitch01443.cs'):

    required.update(re.findall(r'"([0-9a-f]{32})"',re.sub(r'\[TypeId\("[0-9a-f]{32}"\)\]','',(source/name).read_text('utf-8'))))

required.difference_update({'6baf176bc22c4a41ba421bce3fcd9497','6fb10fff27b84bff81746194bf8e38a8','58bcb999b3ad46c289dc68cba9c50bd0','5bc013d5d0144502af6ae14141d6c35e','ba25ca0ee0e74199967db79cfae32318','ab7079a66bff427e82571f2457c04c14'}) # audited external providers

# 035 optional provider selections are not native dependencies. These four

# performance FX identifiers address prefabs, not blueprint-cache entries.

required.difference_update({'45b72087ac2d4554aec4f44be852eddd','94e2cd84bf3a8e04f8609fe502892f4f','79665f3d500fdf44083feccf4cbfc00a','dfc59904273f7ee49ab00e5278d86e16','9353083f430e5a44a8e9d6e26faec248','91ef30ab58fa0d3449d4d2ccc20cb0f8'})

required.discard('79cd602c3311fda459f1e7c62d7ec9a1') # 043 Pillar of Light PrefabLink.AssetId, not a blueprint-cache identity.

required.add('caac251ca7601324bbe000372a0a1005') # Native Shadow Conjuration disbelief regression.

required.update({'17239b298065efc459cffe2220ecb559','38384e0c1e99c2e42ac6ed70a04aca46'}) # Native Lingering Performance and actual Archaeologist archetype metadata.

required.update(['c074a5d615200494b8f2a9c845799d93','14d9089df87a43b696fa9451ca2f0a12','1edbb68422c046f98a86f4726f6721b2','290bbcc3c3bb92144b853fd8fb8ff452','4b7018b1ed4b27140a5e7adfacaaf9c6','c0f4e1c24c9cd334ca988ed1bd9d201f'])

# Retiring authored Atavism must never retire its independent native donors.

required.update(['a67b51a8074ae47438280be0a87b01b6','955e356c813de1743a98ab3485d5bc69','9292099e5fd70f84fb07fbb9b8b6a5a5','1a3b471ecea51f7439a946b23577fd70','d99dfc9238a8a6646b32be09057c1729'])

required.difference_update({'df05fc9b9f394d26ad3a51e21ec6ac06', '22df5ebca04e4c3bb72bd6a032df26bb', 'ad42caf043154c088ebb1fb888f32f84', 'c897a7577c0a4e68b733b3c655c9eb79', '292c89759fa14af383d5b7dbccf72b92', '21606a268ea7453892e23ac549548efa', '9185044273244e208f00845cf44b81c4', '2ebc866b610a465583032c5006f55bc3', '0901ea8f1cae465da272bab8fee863e4', 'c73eb87ced4e42e882c9c73c793b8797', 'ea78acb4d56044c3a6da2e3ddd20006b', 'b2c149bdce2347539d759a74c642c9e3'}) # 036 audited PrestigePlus provider identities, never native dependencies.

required.update({'bc04fc157a8801d41b877ad0d9af03dd','0d3651b2cb0d89448b112e23214e744e'}) # 036 actual Bard book and original Extra Performance provider child.

required.update({'0a8eec9ca5c0dc64795243ab3c55d924','90de6b4f591d1184497fbfbae6e16547'}) # 036 actual Bard per-day and known-spell progression tables.

# 038 real Arcanist exploit / greater exploit / Extra Exploit parent routes.

required.update({'b8bf3d5023f2d8c428fdf6438cecaea7','2ba8a0040e0149e9ae9bfcb01a8ff01d','0a7065e13f4449fa81bfb33199bf7f6a'})

# 038 actual Wizard/Sorcerer/Arcanist native spellbook progression data.

required.update({'33903fe5c4abeaa45bc249adb9d98848','78bb94ed2e75122428232950bb09e97b','d1d296ca4bf8449409fc330a54ad58e1','dbff16956df2eda48a1da5c9617cc836','d232bc78d967a964bac4d4d38e7ca5f4'})

required.add('44be796c5d37bb244aae59d0a005530f') # Actual Arcanist preparation capacity.

# 038 actual Wizard8/9 spell data consumed by native CharGen choice building.

required.update({'8ba9b6e4df4c46a597154e2b8e7e6e4a','c3d2294a6740bc147870fff652f3ced5','198fcc43490993f49899ed086fe723c1','df2a0ba6b6dcecf429cbb80a56fee5cf','4dc60d08c6c4d3c47b413904e4de5ff0','b4afacd337dac4a40a769a567c038ab7','8a7f8c1223bda1541b42fd0320cdbe2b','6101d0f0720927e4ca413de7b3c4b7e5','5e36df08c71748f7936bce310181fb71','9cebb7c46c8d4c55a0e98da9b103fd37','37302f72b06ced1408bf5bb965766d46','08ccad78cac525040919d51963f9ac39','7f4b66a2b1fdab142904a263c7866d46','1852a9393a23d5741b650a1ea7078abc','87a29febd010993419f2a4a9bee11cfc','2f8a67c483dfa0f439b293e094ca9e3c'})

# 037 actual Witch spellbook, selected Agility patron and Slumber hex donors.

required.update({'dd04f9239f655ea438976742728e4909','630ea63a63457ff4f9de059c578c930a','498d7668a6086454cb2def921b89bd8f','08518b2a62446c74b9ae08ee73664047','bdc230ce338f427ba74de65597b0d57a','2d2243f4f3654512bdda92e80ef65b6d','75efe8b64a3a4cd09dda28cef156cfb5','5a38c9ac8607890409fcb8f6342da6f4'})

records=[]; all_ids=[]; link_records=[]; native_raw={}; selection_pools=[]; spell_lists=[]; native_types={}

required.add(chosen_cleric)

required.update(re.findall(r'"([0-9a-f]{32})"',re.sub(r'\[TypeId\("[0-9a-f]{32}"\)\]','',(source/'SpiritWeaponForms.cs').read_text(encoding='utf-8'))))

required.add('e9c2bef81c166654088e27dea05e4fbd')

required.update(re.findall(r'"([0-9a-f]{32})"',(source/'WildShapeCatalog.cs').read_text('utf-8')))

required.update(re.findall(r'"([0-9a-f]{32})"',(source/'TeamworkCatalog.cs').read_text('utf-8')))

required.update(re.findall(r'"([0-9a-f]{32})"',(source/'CooperationCatalog.cs').read_text('utf-8')))

required.update(re.findall(r'"([0-9a-f]{32})"',(source/'CompanionSpellSharing.cs').read_text('utf-8')))

required.add('09d39b38bb7c6014394b6daced9bacd3') # Native Stunned buff for Stunning Assault.

required.update(['92c7e14b4da24549a9219e990b30d723','621523572cfe4dc48ee1bdddba2f4725','9af88f3ed8a017b45a6837eab7437629','ac8aaf29054f5b74eb18f2af950e752d'])

required.update(['0da0c194d6e1d43419eb8d990b28e0ab','422dab7309e1ad343935f33a4d6e9f11','6337b37f2a7c11b4ab0831d6780bce2a','956331dba5125ef48afe41875a00ca0e','e3c01ec2609ca7b42adc2bf37b6a5d8d'])

required.update(re.findall(r'"([0-9a-f]{32})"',re.sub(r'\[TypeId\("[0-9a-f]{32}"\)\]','',(source/'WeaponShiftTransfer.cs').read_text('utf-8'))))

required.update(['a406d6ebea5c46bba3160246be03e96f','934670ef88b281b4da5596db8b00df2f','0e6233b7c8a14f01b80a83731868454c','9292099e5fd70f84fb07fbb9b8b6a5a5','7e11b375ec4e48f09642b14ca5a58346','a97ef112430b4104956b737ddaede57b','14d16d7ff88d41c08fb4cb4be26383b6'])

required.update(['19bb148cb92db224abb431642d10efeb','ae6af4d58b70a754d868324d1a05eda4','610d836f3a3a9ed42a4349b62f002e96','ab3559a3cb9dcef49b8b529590e216d6','8d24793851c6b2e4688e785f6954de68','f3f984b315f0439418ce8358bc52af7b','508cba5e167e33c439515713463bd715'])



required.update(['ec2da496c7936e14c9a28ce616a6b4cd','4eacfc7e152930a45a1a16217c35011c','a33b99f95322d6741af83e9381b2391c','04430ad24988baa4daa0bcd4f1c7d118','43d1b15873e926848be2abf0ea3ad9a8','913b9cf25c9536949b43a2651b7ffb66','53f7237f5b1447bb851ba68045a00e41'])

# Psychic058 native dependencies: actual native GUID index, original dependencies retained.
required.update(['02b187038a8dce545bb34bbfb346428d', '08323922485f7e246acb3d2276515526', '0937bec61c0dabc468428f496580c721', '12fb4a4c22549c74d949e2916a2f0b6a', '137af566f68fd9b428e2e12da43c1482', '14ec7a4e52e90fa47a4c8d63c69fd5c1', '15a04c40f84545949abeedef7279751a', '1a5e7191279e7cd479b17a6ca438498c', '1d48ab2bded57a74dad8af3da07d313a', '1e2d1489781b10a45a3b70192bba9be3', '2b044152b3620c841badb090e01ed9de', '3e4d3b9a5bd03734d9b053b9067c2f38', '40ec382849b60504d88946df46a10f2d', '450af0402422b0b4980d9c2175869612', '4e42460798665fd4cb9173ffa7ada323', '55f14bc84d7c85446b07a1b5dd6b2b4c', '734a29b693e9ec346ba2951b27987e33', '740d943e42b60f64a8de74926ba6ddf7', '75039846c3d85d940aa96c249b97e562', '886c7407dc629dc499b9f1465ff382df', '8a28a811ca5d20d49a863e832c31cce1', '8d6073201e5395d458b8251386d72df1', '8eead52509987034ea9025d60cc05985', '956331dba5125ef48afe41875a00ca0e', '9934fedff1b14994ea90205d189c8759', '9e1ad5d6f87d19e4d8883d63a6e35568', 'a9b8be9b87865744382f7c64e599aeb2', 'c3a8f31778c3980498d8f00c980be5f5', 'cf6c901fb7acc904e85c63b342e9c949', 'd316d3d94d20c674db2c24d7de96f6a7', 'e3f460ea61fcc504183c7d6818bbbf7a', 'eabf94e4edc6e714cabd96aa69f8b207', 'f08a7239aa961f34c8301518e71d4cdf', 'f180e72e4a9cbaa4da8be9bc958132ef', 'fa3078b9976a5b24caf92e20ee9c0f54', 'fd4d9fd7f87575d47aafe2a64a6e2d8d'])

# Psychic058 required clone closure: hydrate original native facts before base registration.
required.update(['008be7892c994b7198e49567eace329c', '011f6f86a0b16df4bbf7f40878c3e80b', '0151bcd6b3ae4090ab93ce36766fcb5d', '027cb4deb3e668b46904dde64f5d06b1', '02bb52709dea75f44bc9c24a379e1a6f', '0344dab893a049b3bfccbf015582112d', '0377fcf4c10871f4187809d273af7f5d', '03b305962d8a9c2478deb76e1015fc9a', '04314be4be2b7f444883ca8caf78a8a8', '0669e4beb1d0e694eb203d129dbde969', '073cb12cc07871a47860b3c75e536382', '07777880a89d434c896660e4a521df92', '08c10a3914fa5bf459054a34a7541704', '09605c9c3199448f821b2ed9d2a68c54', '0a385e9401dcc8e49ad08c9b9b62fdf7', '0ae7767105d6d134382f94517d982bc5', '0c10b4bbcbcb6a942a77a6804682c7d8', '0d51a2ff0a6ce85458309affbc00b933', '0e370822d9e0ff54f897e7fdf24cffb8', '0f49bfc6c59242429f948061f0c432e1', '0fd21e10dff071e4580ef9f30a0334df', '1000fb244ea388c49978151cbddc0080', '1008092e869931943a7bc45c5dfadd05', '10b5031a2182416b9a3341031e22bf23', '118fdd03e569a66459ab01a20af6811a', '11c6495354ad4ac09a57882646917c64', '128eb2e986ed425b9536b03b24be9182', '13e0e8560db9471aa7c0ff18be3e633b', '13e4f1dd08954723b173335a54b48746', '13f9269b3b48ae94c896f0371ce5e23c', '14359e9c35a42ee48b8c31b4424a8d3f', '152a21a62971404b9c737ca0c57881da', '15bac762b599b7e42824c333717d79d9', '1633025edc9d53f4691481b48248edd7', '1658b352bc364bee85026e8d0dc75cc1', '174e9be5edaa41aeba7aa07cca81df6b', '17903369fcc1aa241a87ec264597aed5', '19250e5a12db1fc448970b81e0f38922', '1993114ecda289d4e8dc681530cbe0b8', '1a2f020861cf1d843ab99170284b5ac5', '1a74716a5b0217a4eb922856c01e80cc', '1ac79088a7e5dde46966636a3ac71c35', '1c2fdba3b33dacd41afd5b74d84c7332', '1ca71abc840453a4ab9dcfcbbaa09794', '1d39a22f206840e40b2255fc0175b8d0', '1dc60bdbf5843f342aaa5e838b66e43a', '1dc6739a4d9543468d67d94a2bd4306e', '1dd712e7f147ab84bad6ffccd21a878d', '1dd9f07805d548ea9f8fee103c2446ff', '1e2e6dbddf785194ab3313ae429622da', '1ef8d7ab3ca4795498ff446cb027e2f3', '1f99fbb3b1b445d6bfbaf3588af171cf', '1fa738778f4811247befeaa9b19da91f', '200baf16628d3ab4b993094b51df5891', '204a74affae72d54984fb533704caf72', '206ce2ab899449b9b23e73654588251d', '20e740104092b5e49bfb167f1670a9de', '213a8480d22206b45acbfa0619ca5aaf', '2174867278aab684d87744a6dc1c753c', '232fe914c22744c4ea3e050901bda424', '23c3fb906c6346440b4e7d92fc25319b', '24700a71dd3dc844ea585345f6dd18f6', '2483a523984f44944a7cf157b21bf79c', '25e6b7c3685d14645bbd99dc19fba40b', '26642195b46c9f74c95b841478e410f1', '271af8eb5d6ccd04899f548380bff006', '277b0164740b97945a3f8022bd572f48', '27d646fc04ff43f48af74300a4a58f9e', '28384b1d7e25c8743b8bbfc56211ac8c', '28ccd31b222f6954490f4c19c5c5576b', '29229b1722a94d709a5a59a80d6348e5', '298edc3bc21e61044bba25f4e767cb8b', '29f6945052434f4a8392b511099a3ce6', '2aabe63fb03518a41bbcad36dd70a71c', '2adda18522f446a699fbe1dcd5fb994d', '2bc8d4bb8baa23a4b84ef34945d13733', '2ccffe3c3d16d20438606fc1e3b7c7ca', '2cf2b133203b6404c8f0927c854c444c', '2f206e6d292bdfb4d981e99dcf08153f', '2fa73e1ff609a8e4d8119d8b66eb39aa', '31387856beae48349bce207c526d4999', '318ce88da1832744c87fd15be5010dc0', '31c18eae013b09c4f9ee51da71a2d61c', '32276246f26cff74aa70f449896bec54', '3239a085ec94fb545ade6a41607c3049', '32c4d277007aed74c905779cd04a6fed', '32f2bc843effd9b45a0952a3cffbbe9f', '3335c5ea5b84e7f499498382d9dd33a5', '34762bab68ec86c45a15884b9a9929fc', '34b0220ab418bc14c837d9985bc3d860', '34fde71198d30094aa133546e8cf8733', '35ca616790867ab428b5b25f7efa626f', '3642547f895815f4786b7f2ac716e03e', '36db7807451714043894c1ccbbed0db4', '37f4f8bdbcfb97641979b52bfb6d30db', '381c28b9a35b80547b8abcdae51b05e6', '39d6ef0cb9794afbb5af3cae7288029d', '3af88e49224cf9244ab7477c08427e03', '3b0f03e24ad0b7243b5973c3d8ab7af5', '3b7cf6307d3e61545a977c9f4156e12e', '3de06230f02eae347af5f9cf4a4e3086', '3de922e9946e9804a95500ad97ba2cb2', '3e48b044bc1145bda44d1a3293527b10', '3e9b859762db1cf43a23430c7fd6962f', '3ef666973adfa8f40af6c0679bd98ba5', '3fb96c96ac146294ba1e92fdb9509a5d', '3fb9e9a6408589343bc8bfc3fd1610e5', '3fd30003d9a4a5d4eb05ddb6eeb7c902', '4048cff852c8413d85e86d68be7e02a6', '408df3003e1b420bb4397c495382e580', '41281aa38b6b27f4fa3a05c97cc01783', '413c2458f8e7a2c48a47723271a1fbb1', '4152f08d9f58b664d972349af8be70a5', '41f68e5c879e440991740a03f5ed7541', '428006e0c196ffc48b54a4404b728c51', '445c696bead5140459909ffbed14750e', '44e3c2b22dfa4afead6c2b9897d22f7c', '45472076126dc154f9386b0bc4d8416c', '45628d9632fd42979c10f4eebad6fb41', '456a15ecac9dcaa4499b85eede4cb7b1', '46b66a2823f048baaf51d6a8b70846c3', '48ebaa4fde5c0ff47b466a1cba740329', '49639dffba041a04f906aef5261ad746', '4ae78c44858bc1942934efe7c149d039', '4aeb3279e7b0a384bb1f294dc2a6811a', '4af0f63ebf3f4eb08dade5a8709ff5a5', '4bf1e3da22a4fe44f8a516cc24e6ef79', '4e15565b830d4b841b59e701d3395371', '4ee08802b8a2b9b448d21f61e208a306', '4f9c07f21ec742b6882d3d1453a9c2df', '4fb54a7eafb647d2a82347437a2b652d', '4ff45d291d0ee9c4b8c83a298b0b4969', '5138e24fbeca1594db6c5455301cf977', '544056cc5aca4d03823e13c5f8873c88', '5566971fdebf7fe468a497bbee0d3ed5', '55edf82380a1c8540af6c6037d34f322', '56b70109d78b0444cb3ad04be3b1ee9e', '56dc07bd2c15488f87bfc53a7415ec24', '575fd6cafe7526a4aadbd1ce2ce94e68', '579ab5b0c5bbce445a5a9bee1b1fe057', '589c977574ea8dd48bc26b5020533e82', '59895c24b70e45a692ffb0b5c4f41f1d', '5a21b25be7bb43e0b94646511afadfc6', '5a73194191ed1494695d372894a4fafd', '5c9ef8224acdbab4fbaf59c710d0ef23', '5cc2e705f83549840ba6e1e0e8033216', '5eb779547b384624a5a1d02669b32d8b', '5edfe84c93823d04f8c40ca2b4e0f039', '5f1116624923df74ba19c2ee42f55a6e', '5f94b6d19093f60499f7532f1b084b06', '6026db1a1a84ac14d9e94404af3baf0a', '606312eee28917e4fb384170721102ee', '608dd115b3b0fba4ab511f448bc798f8', '60eb20b9d1077ed4f8f8a9df5490a208', '61179501dfdfaa84d970d9ad2957b680', '61271a59038390c488c313f7a0aee6ea', '61477e5901524671b2cd8401b82d8878', '6170150bd050e234d9ab50b3e3b87bd1', '61bbfd905edf4040a125407df8f450e3', '62582b4636218d74db34606f872c9c05', '62930491ed8247a18bd6cf1e3d5ae4d8', '63063a8ab91bcbc44a13294227580e84', '638db1905268b3845825ffa194b7415f', '64e647c10eece1545b00a442330f49da', '65eb73689b94d894080d33a768cdf645', '67bdd8baf2864a4ba25fa0c2892579a4', '6828cbd9b774f624fa9af24a775996f2', '6871149a90e278f479aa171ee8bb563e', '690d26bcc63c4a2e912b6104d1d531e7', '696073ae669097a4ca3a5c8765fa4a3f', '6aaf11aa06ae0e7499a71b79725828df', '6ab67ef0e26be9449acbc3775ed6f5de', '6b19f5aaca52493faf8ced3d344f32c1', '6b5ecc87dd4948c499729fe28ba4b45e', '6c2d0b601e95e304780f9fab8c905257', '6c9c621ceb044543aa782515ed334798', '6e359f50e7a2b79498fb4abb6814d798', '6e4cd4c19c1db674ca6e2635594927ae', '6e649d13e6c78554bb35757e0744a713', '6ea5a4a19ccb81a498e18a229cc5038a', '701690d8339e81f439bb025038e36a96', '707f3cc2e103a8d40ab79ff49664b2bf', '7081934ab5f8573429dbd26522adcc39', '7283344b0309d8e4cb77eb22f1e7c57a', '7306d5c84435403abc068ceb8f8e5c90', '73283d50ed8b4385ba5615238a3c79a6', '74d3374c5e41f8c408c85625fbe12385', '759749854f9f4256867073e1ffca9494', '76734c81141746eeb9e122bfe3f25bd5', '7687aa3764cb651448a50016e1758069', '7775c915661f8fb449c38dd873524389', '784af3f699244467953696d27a4b9a9a', '78937c3583ef93d4db833a5da8dd0859', '792133a0b16855041ad8b4d199059527', '796b2c438fbb3414eb2cd041b625e8af', '7c4bbfe3b089a8a4ebcd2401995230a4', '7ca6ae35171a429bbcfb375ab657776f', '7d9d68168aa358c419217eebae32c1f7', '7df9329013baaec4fac20ae08bc1f849', '7e0c12deb10a41b8b9137fedb5413347', '7ed4cbc226539d6419d6c60320bfd244', '7ef78403a4004f1aaec7b5d150cc76c7', '7f2fa59c5b05df849a33e8a49e658137', '80316578ceb59b841bb82f05dae37c3e', '8177ab918285fb34c8dce57c72801c0c', '82429f7b861ee0a4c9bacf08a23ce1ee', '830da47445994f02b66398f065c9b585', '83ed8d5c1e4ed9045874494c0fe2b682', '84ae955af09809b4ea31a2c719c68377', '84c42fea967a2a8499ceeaef3a6416b8', '854d02f9bf2949139635b6e2bdbabeb0', '86260f0689844d5db053a95b4be11ddb', '864e29d3e07ad4a4f96d576b366b4a86', '86b46db32b194f289eb9535528484c5f', '871e6b0bb9d050743b98b78990ae1cff', '875aac46f5e879f4c9ec3ba46847d86e', '883fdcd6653a4d3aa5ff7068820cea9c', '88453bd5448456748ab531cddad38721', '89717411cbd84b06bf9d92940b3d825e', '898c1e3fdec34ccebe3702c78a2f20e8', '89b501348d054d6408a930efd6105200', '8a18b8eec185cd24eb55c3457a7f69b1', '8a43b4c0a59f7bb479d2af66d9a43ac5', '8af258b1dd322874ba6047b0c24660c7', '8b4dfaf9c9b46574f85c16c8d638e12e', '8bc2f9b88a0cf704ea72d86c2a3e2aef', '8bc9cee033c04fd7bb72c49fe0cd2472', '8bf97ce6a52d7274197a6c1911378b27', '8c2c9a379ec44c26a4f02fd2f5e0b952', '8cb3816915b1a8348b3872b964a2fa23', '8ce87f6f5d5c8064c801bf0469f79470', '8cf657c7d70765a4ab58c3ab619d181a', '8d4357118c75a5746802a3582a937376', '8dca02b163c8f904c8780afd09b968fe', '8de52f7aa6052a0498875e0d834330af', '8e85b6347caa4e84fa67d71f65a6a990', '8ea9aaf89a2fa5b42976deae19d0d562', '8eb2fdcfecde4058a14d19f16e46bd4d', '8ecf6bce18c60c8478458b93e98f3607', '914b04c279e3d3447848fecf5796cdcd', '915ec70c04d0f4944b4d783a6644dbd3', '918555c021b3a2944beed35df53b4c56', '934e60c19112413aabf5d68127d108f5', '93b62f230b22101419e7e7b0c2c12733', '94064ed53b1020247941ac70313b439d', '94b1aa3985e6b0942a1254ae6f49cd1f', '94be432b7f80427b955db7b354274b2c', '955e70790e3255d4eb362c6d12400e52', '95b3e53bac4ae234a86868ca9820235b', '96c174b0ebca7b246b82d4bc4aac4574', '9726b5f26a2f44bb9fdf944bdf69a24e', '98a46e8da1dca9f47b41b9d71d579628', '98ac795afd1b2014eb9fdf2b9820808f', '99028133744fa6a4e853489362a16cf9', '99bf7cf258875ac418a373fce573bf04', '9a358a71928952547b885389f5ba2a82', '9a41e6d073b42564b9f00ad83b7d3b52', '9a71a8b2b2c14d12b8cb96399ca7a9a9', '9aca88a627afc724bb167b05aba2f6a4', '9b70e0d2423352e429bfcbb978f4c687', '9c3761b9f48f69849ad78873c5a12147', '9c6f753d192146468362e13d7fc977f3', '9c747d24f6321f744aa1bb4bd343880d', '9c7881ebbb9c96944ad18adca92fdb4c', '9c7b33404dc0ac1449ee2732657b722a', '9d277308f2d12af428f7726d58152ca5', '9e305d3c312fa0f4296e3174f0a7cfd8', '9e6e778d3527432c9037be84fa87d42d', '9ee0f6745f555484299b0a1563b99d81', 'a13839f75ac30cc48945c96ef1c98e1c', 'a41c5b7817264a00b80693db852e12f7', 'a42c49fcb081bd1469679e4f515732c8', 'a4418aa778cb4ebd965d78af19becced', 'a5a6f915d13fd994fb109473032d7440', 'a5a8a708d6074246824ffa7f17af40aa', 'a5cc3a9960e4105458366783d6f703d2', 'a64a3f4c8370e3646ada9e140e28f7fb', 'a708945b17c56fa4196e8d20f8af1b0d', 'a8e7ca242395c3b49af5a3dbc9dee683', 'a95cf8129b1a4741a2a7cf1eb011b21e', 'a9fd0f634edc36c4aa1014c0c09f354b', 'ab6b5395f4984bf68829c9840b18d9a4', 'ac7753d72b0b7264982c2b6670fa2a2e', 'ac812a821f74aa44cb854e8cc3553948', 'acb260a9f839dd24d9434ceb0fc59238', 'ad84149c307247a381f17cc42acc38fa', 'ae21c5369252ec74aa1fee89f1bc1b21', 'ae9e3a143e40f20419aa2b1ec92e2e06', 'aeac272bf357c1247a51e9c56af7193b', 'af9a8663de9346d1807ea92bed9bc930', 'b1326a7a72fae4c4996339e14715c08d', 'b163d3db48c659048a845bc6f55fd7fb', 'b2eec7193059fbc4a92076d6b8a83eea', 'b448a9c0ff27b6846b9c676f6839e907', 'b45c0db01c501864496f59b04a06b17b', 'b4a3dd550b4f40d0bebbda3cd0ebc3ae', 'b60f8b93d3d1d26439c1bb48fd461a3a', 'b636127171ae4c20b8e3f4d531c89905', 'b776c19291928cf4184d4dc65f09f3a6', 'b7b7b27048704b90b40a2c9db6a0732c', 'b84abc3531ed5674284ef0ba4aafcd3b', 'b865aa952f8fdd348a635fcd276b758f', 'b921af3627142bd4d9cf3aefb5e2610a', 'ba0e5a900b775be4a99702f1ed08914d', 'ba27c690b06efad42ba7e5ca408d780b', 'ba2951ffa6bcbfb4c83cdfb6919862c5', 'bb23c508e83d44741a971c34959daa81', 'bb308edc67834597814baec241006d44', 'bc0890817bb28fe4a86094fe57cd40fb', 'bd48322a4e258b8418106dcc6459e024', 'bd59614d30bcadd46bd56aabe0de819f', 'be190d2dd5433dd41a4aa00e1abc9a5b', 'be35623d2c561c649b98a1794216e9f9', 'be3d454ea70a8bb468b0a8a087e7e65b', 'bef38216043d3584cb838bf578aa0220', 'bf73a2b70b6fac54e891431cf6c7d8eb', 'bfe8300529214c7ca4ae0024f834f085', 'c039ac164bbc76f4594d4fa0b81ed3ca', 'c03dc59e2ce30484b82e7aead6d10471', 'c19e1e909cd34ab48574a4e19c91b755', 'c2b7fa6ad976b084db711433b6f17716', 'c3365d5a75294b9b879c587668620bd4', 'c3a13237b17de5742a2dbf2da46f23d5', 'c42ae8f9652bbc14eb13b31d12d20f8a', 'c4732ca5b0af7214a98f9b4dafe87da9', 'c4aa7ad69a9908740a8d6994f1cc8f54', 'c57c0b9c0b624dd88bf6e69bc499a58c', 'c5a35842eaf1409d892ca0ce9ad79063', 'c632e516f383442489abb8ca810d639a', 'c6ae677c7c1eee74bbf3d44ce25961b8', 'c73b37aaa2b82b44686c56db8ce14e7f', 'c8719b3c5c0d4694cb13abcc3b7e893b', 'ca4ecea1b0c0449fbf54d86ddbfe74aa', 'caae1dc6fcf7b37408686971ee27db13', 'cb502c65dab407b4e928f5d8355cafc9', 'cc03b87fd3a797f4e953773db424cee7', 'cd65a240f32aa7441a0f10b1a86ed520', 'ced46f0e08175bb499c4a8db3c98851a', 'cf594fa8871332a4ba861c6002480ec2', 'cfbec3b1709b28d4dac24aba45c8c683', 'd0a5cedfd497f3b4f9581b6066d9043b', 'd1683a4035c05c64b9e42993f19be24b', 'd25326011c45a5942bc12f5d4401d4ae', 'd306e052d2f85b24cb5082951459045e', 'd33e0b20052b407e98db4ec022d23fdb', 'd37ded3ae3bd4936b2c038c944501477', 'd41b875460534c07a49fb13ac935f087', 'd4675c5e630e4602bb96e295becfd5cd', 'd4c929bffb80464bbe6daf1471088bf8', 'd4f51beb3daf64748badf5e9e2d91666', 'd57301613ad6a5140b2fdac40fa368e3', 'd7b6b2797253a5c478c957ba30bede47', 'd8d451ed3c919a4438cde74cd145b981', 'd8df8f8ad7149824e8e60dd3453557d2', 'd9eaeba5690a7704da8bbf626456a50e', 'da129e268aaf38643a4fa2d9e39adc7f', 'daf27e1f12e736d4294b525489e99de4', 'db611ffeefb8f1e4f88e7d5393fc651d', 'db8b731dfb6c9974b8bf67a19aefe225', 'dbad8a46a6278e24785e2b5b24c23de0', 'dca4ded269a349a590b773b591f9689e', 'de2ee280f4b340748ac5c56c35f01648', 'de4f4b6aa7f62204c95f5d03ac0bc459', 'de5b285a0a8e1e745a0cc9bf02045123', 'dfc2aa71bdfebcc49a6302cce4ce4bbf', 'e021f1e080cda2c468f572e04f95fda1', 'e027e949f6e54e6591b309fff92bf0b7', 'e34406825b1d5b145a12af91f1844df3', 'e3f708cb970e4e7c882f8b86d3c78e7e', 'e53754efdf844a62a989e5098059b8c7', 'e6233e8be3354d098eb8ed81a7d0ee7a', 'e7a401dc332dafb4eb4878b0217e0430', 'e8096942d950c8843857c2545f8dc18f', 'e8e8ac4627f94b0c9896a6ca0a02f9bb', 'e91c22f048894ddea1f6b7f417404920', 'e9b36feeacdadfd44af1ba70f7ca7539', 'e9e7d67d6ffb8ad4ba601fccffba4d6e', 'ebc313fce3a148bd8ea7e64257d28bc7', 'ebdca04cd5b9c6443bb6281dd63bebab', 'ecfaa8983906848479648ab0659d98f7', 'ed01d50910ae67b4dadc050f16d93bdf', 'edd19d49f6392e543a827f1cda927068', 'edd8da6ce9ef45828972ba90a6510153', 'f029268a6639a0745a849e5147734088', 'f068f20ec5274c82aa9cec0cd2835267', 'f18a9ed7bcba738479b97162b9c77f46', 'f2be3d538b5d75c409289d35399723c4', 'f2f6861b5daf4a24abd9d075896842c5', 'f35e15b1fdff0c54087746c2da80a053', 'f3e2fc2d692c40e8b24f14c40dac5297', 'f4f94ae005b810e4a99082fd1e367bb8', 'f643b38acc23e8e42a3ed577daeb6949', 'f6b67c9c82a64db3b8937d74230390d5', 'f6dac9009747b91408644fa834dd0d99', 'f6ef95b1f7bb52b408a5b345a330ffe8', 'f75d3b6110f04d1409564b9d7647db60', 'f766db0dcd17aaa44b76181bdf85fee9', 'f779120fd8e69ca48928e2457ef2a2a1', 'f77d6039bf7243ecb46798a22f2ddd71', 'f85a3554e6dcc8448b189f867d082820', 'f8a32c60ae1f878408b525bb967ef48c', 'f9565a97342ac594e9b6a495368c1a57', 'f9c038203b03ea14498d811b6a61af0a', 'fb135581caaf42e89d31536c841bb49b', 'fb890fa3374c38f42b08402688d984cf', 'fcd758e6607d4f91957f5962ab1235d4', 'fd13e9efee08db448974fe0263eb96c8', 'feff1b48642214c45a6cb13f683f3552', 'ffeb2963c18cf5d4b9e5c7a1e8ae73d4'])

# Psychic058 actual stock metamagic source facts, independent of ordinary pool filters.
required.update(['46fad72f54a33dc4692d3b62eca7bb78', '7f2b282626862e345935bbea5e66424b', '85f3340093d144dd944fff9a9adfd2f2', 'a1de1e4f92195b442adb946f0e2b9d4e', 'cd26b9fa3f734461a0fcedc81cafaaac', 'ef7ece7bb5bb66a41b256976b27f424e', 'f180e72e4a9cbaa4da8be9bc958132ef', 'fbf5d9ce931f47f3a0c818b3f8ef8414'])

with zipfile.ZipFile(pathlib.Path(sys.argv[1])/'blueprints.zip') as archive:

    for name in archive.namelist():

        if not name.endswith('.jbp'):continue

        raw=archive.read(name)

        identity=re.search(rb'"AssetId"\s*:\s*"([0-9a-f]{32})"',raw[:500])

        if identity:

            guid=identity[1].decode();all_ids.append(guid);native_raw[guid]=(name,raw)

            native_type=re.search(rb'"\$type"\s*:\s*"[^"\r\n]*, ([^"\r\n]+)"',raw[:700])

            if native_type:

                native_types[guid]=native_type[1].decode()

                if native_types[guid]=='BlueprintFeatureSelection':

                    selection_pools.append({'name':name.rsplit('/',1)[-1][:-4],'blueprint':json.loads(raw)})

                if native_types[guid]=='BlueprintSpellList':

                    spell_lists.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

            if guid in required:

                records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

            if guid in link_ids:

                link_records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

# 0.14.28 donor graphs: real school progression features/metamagic prerequisites,

# wild-talent fire prerequisites, poison effects and kineticist settings.

seed_files=('Inquisitions01428.cs','PurifyingFlames01428.cs','WizardSubschools01428.cs','EnhancementSchool01429.cs','FlameJet01429.cs','AngerInquisition01431.cs','FerociousBeast01431.cs','WizardSubschools01431.cs','KineticUtility01433.cs','SpiritualGuardian01433.cs','KineticInvocation01433.cs','RagePowerMigration01434.cs','SuperstitionRage01434.cs','AntimagicRage01434.cs','WizardDiscoveries01435.cs','BardMasterpieces01435.cs','TimeStutter01437.cs','Invoker01437.cs','WarDrummer01441.cs','Sunsinger01443.cs','UrbanSkald01443.cs','RogueSwashbuckler01443.cs')

frontier={'d56c44bc9eb10204c8b386a02c7eed21','08518b2a62446c74b9ae08ee73664047','498d7668a6086454cb2def921b89bd8f','bdc230ce338f427ba74de65597b0d57a','2d2243f4f3654512bdda92e80ef65b6d','75efe8b64a3a4cd09dda28cef156cfb5','5a38c9ac8607890409fcb8f6342da6f4'}

seed_files += ('AuraRadiusVisual01458.cs', 'MutationReach01458.cs', 'PhysicalMutationToggle01458.cs', 'PsychicAdvancedRuntime01458.cs', 'PsychicCompletionAmplifications01458.cs', 'PsychicDisciplineRuntime01458.cs', 'PsychicRemainingDisciplines01458.cs', 'PsychicTexts01458.cs', 'WillOfTheDead01458.cs', 'PsychedeliaCognatogens01458.cs')

for name in seed_files:

    frontier.update(re.findall(r'"([0-9a-f]{32})"',re.sub(r'\[TypeId\("[0-9a-f]{32}"\)\]','',(source/name).read_text('utf-8'))))

seen028={r['blueprint']['AssetId'] for r in records}

# Bound traversal, avoid expanding all class/domain selections and spell lists.

for depth in range(3):

    upcoming=set()

    for root_id in frontier:

        if root_id not in native_raw:continue

        # Anger only checks these deity identities. Their AddFacts graph is an

        # unrelated, broad sacred-weapon catalogue and is not a donor effect.

        if root_id in {'8f49a5d8528a82c44b8c117a89f6b68c','04bc2b62273ab744092d992ed72bff41','c1c4f7f64842e7e48849e5e67be11a1b'}:continue

        root=json.loads(native_raw[root_id][1])['Data']

        if root.get('$type','').endswith((', BlueprintSpellList',', BlueprintCharacterClass')):continue

        if root.get('$type','').endswith(', BlueprintFeatureSelection') and root_id!='8c3102c2ff3b69444b139a98521a4899':continue

        root={k:v for k,v in root.items() if k in {'Components','LevelEntries','m_AllFeatures','m_Features'}}

        for guid in re.findall(r'!bp_([0-9a-f]{32})',json.dumps(root)):

            if guid not in native_raw or guid in seen028:continue

            if native_types.get(guid) not in {'BlueprintFeature','BlueprintParametrizedFeature','BlueprintFeatureSelection','BlueprintProgression','BlueprintAbility','BlueprintBuff','BlueprintAbilityResource','BlueprintUnitProperty','BlueprintCharacterClass','BlueprintArchetype','BlueprintSpellList','BlueprintActivatableAbility'}:continue

            seen028.add(guid);name,raw=native_raw[guid]

            records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

            upcoming.add(guid)

    frontier=upcoming

# The 036 real CharGenSpellsPhaseVM path enumerates the complete original Bard

# list. Include its native abilities and bounded effect dependencies rather than

# manufacturing substitutes or patching the native UI/logger for missing data.

bard_list=json.loads(native_raw['25a5013493bdcf74bb2424532214d0c8'][1])['Data']

bard_frontier={g for g in re.findall(r'!bp_([0-9a-f]{32})',json.dumps(bard_list.get('SpellsByLevel',[])))}

bard_seen={r['blueprint']['AssetId'] for r in records}

for depth in range(4):

    upcoming=set()

    for guid in sorted(bard_frontier):

        if guid not in native_raw:continue

        name,raw=native_raw[guid]

        if guid not in bard_seen:

            bard_seen.add(guid)

            records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

        data=json.loads(raw)['Data']

        if data.get('$type','').endswith((', BlueprintSpellList',', BlueprintCharacterClass',', BlueprintProgression',', BlueprintFeatureSelection')):continue

        upcoming.update(g for g in re.findall(r'!bp_([0-9a-f]{32})',json.dumps(data)) if g in native_raw and g not in bard_seen and native_types.get(g) in {'BlueprintAbility','BlueprintBuff','BlueprintFeature','BlueprintAbilityResource','BlueprintActivatableAbility','BlueprintUnitProperty'})

    bard_frontier=upcoming

# The 0.14.30 opposition regression exercises every actual school option, not

# reference-only placeholder features. Follow this one native selection's own

# members without expanding unrelated class or spell-list graphs.

opposition_id='6c29030e9fea36949877c43a6f94ff31'

opposition=json.loads(native_raw[opposition_id][1])['Data']

opposition_members={ref.replace('!bp_','') for field in ('m_Features','m_AllFeatures') for ref in opposition.get(field,[])}

required.update(opposition_members|{opposition_id})

opposition_seen={row['blueprint']['AssetId'] for row in records}

for guid in sorted(opposition_members|{opposition_id}):

    if guid in opposition_seen:continue

    name,raw=native_raw[guid]

    records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

# Faith reuses native conversion casts and touch deliveries. Resolve these

# component references from game data instead of manufacturing healing effects.

seen={r['blueprint']['AssetId'] for r in records}

# Template resources, daily smites and their effect buffs use real game data.

frontier={'ab3559a3cb9dcef49b8b529590e216d6','8d24793851c6b2e4688e785f6954de68'}

frontier.update({'c610310d31414edabcedf0c8a6fe32c4','1bb6f0b196aa457ba80bdb312dc64952','32429740d6a5470aaaa02f20d61e43d3'})

for guid in list(frontier):

    if guid in seen:continue

    seen.add(guid);name,raw=native_raw[guid]

    records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

for depth in range(3):

    next_frontier=set()

    for root_id in frontier:

        for guid in re.findall(r'!bp_([0-9a-f]{32})',native_raw[root_id][1].decode('utf-8-sig')):

            if guid not in native_raw or guid in seen:continue

            seen.add(guid);name,raw=native_raw[guid]

            records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

            next_frontier.add(guid)

    frontier=next_frontier

for row in records:

    for component in row['blueprint']['Data'].get('Components',[]):

        kind=component.get('$type','')

        if 'SpontaneousSpellConversion' not in kind and 'AbilityEffectStickyTouch' not in kind:continue

        for guid in re.findall(r'!bp_([0-9a-f]{32})',json.dumps(component)):

            if guid not in native_raw or guid in seen or guid=='67819271767a9dd4fbfd4ae700befea0':continue

            seen.add(guid);name,raw=native_raw[guid]

            records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

# Pageantry uses native Skill Focus entries for Wrath's actual skill set.

for selection_id in ['c9629ef9eebb88b479b2fbc5e836656a','59e7a76987fe3b547b9cce045f4db3e4']:

    selection=next(r for r in records if r['blueprint']['AssetId']==selection_id)

    for ref in selection['blueprint']['Data']['m_AllFeatures']:

        guid=ref.replace('!bp_','')

        if guid not in seen:

            seen.add(guid);name,raw=native_raw[guid]

            records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

# Favored weapon fixtures include native deity starting-item references. Newer

# deities use actual item blueprints instead of a CategoryItems enum entry.

cleric=json.loads(native_raw[chosen_cleric][1])

cleric_book=cleric['Data']['m_Spellbook'].replace('!bp_','')

cleric_table=json.loads(native_raw[cleric_book][1])['Data']['m_SpellsPerDay'].replace('!bp_','')

for guid in [cleric_book,cleric_table]:

    if guid in seen:continue

    seen.add(guid);name,raw=native_raw[guid]

    records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

deity_ids={ref.replace('!bp_','') for ref in next(r for r in records if r['name']=='DeitySelection')['blueprint']['Data']['m_AllFeatures']}

for row in list(records):

    if row['blueprint']['AssetId'] not in deity_ids:continue

    for component in row['blueprint']['Data'].get('Components',[]):

        if not component['$type'].endswith(', AddStartingEquipment'):continue

        for ref in component.get('m_BasicItems',[]):

            guid=ref.replace('!bp_','')

            if guid in seen:continue

            seen.add(guid);name,raw=native_raw[guid]

            records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

# Real weapon types are needed by damage/proficiency checks, not only the item shell.

# Double weapons' second ends are separate native items. Include those shells

# and their types so private-copy fixtures exercise the same complete graph.

for row in list(records):

    if not row['blueprint']['Data']['$type'].endswith(', BlueprintItemWeapon'):continue

    ref=row['blueprint']['Data'].get('m_SecondWeapon')

    if ref:

        guid=ref.replace('!bp_','')

        if guid not in seen:

            seen.add(guid);name,raw=native_raw[guid]

            records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

extra={r['blueprint']['Data']['m_Type'].replace('!bp_','') for r in records if r['blueprint']['Data']['$type'].endswith(', BlueprintItemWeapon')}

extra.difference_update(r['blueprint']['AssetId'] for r in records)

with zipfile.ZipFile(pathlib.Path(sys.argv[1])/'blueprints.zip') as archive:

    for name in archive.namelist():

        if not name.endswith('.jbp'):continue

        raw=archive.read(name)

        if any(x.encode() in raw[:180] for x in extra):records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})

# 01444: append an independent, bounded archetype dependency closure after the
# established extraction, so new roots never prune prior fixture paths.
new_roots={"e7c530f8137630f4d9d7ee1aa7b1edc0"}
for filename in ('TerrorWeaver01444.cs','PrimalSorcerer01444.cs','Rake01444.cs','CourtBard01445.cs','NatureBondedMagus01445.cs','SageCounselor01445.cs','GraveWarden01445.cs','DrillSergeant01445.cs','Overseer01447.cs','Phantasm01447.cs','SpellSpecialist01447.cs','MagaambyanTelepath01447.cs','InfectiousCharms01447.cs','BloodlineVariants01447.cs','BloodlineAnchors01446.cs','BloodlineMutations01446.cs','Admixture01446.cs'):
    new_roots.update(re.findall(r'"([0-9a-f]{32})"',(source/filename).read_text(encoding='utf-8-sig')))
new_seen={r['blueprint']['AssetId'] for r in records}
new_frontier={g for g in new_roots if g in native_raw}
for depth in range(4):
    upcoming=set()
    for guid in sorted(new_frontier):
        if native_types.get(guid) not in {'BlueprintAbility','BlueprintBuff','BlueprintFeature','BlueprintAbilityResource','BlueprintProgression','BlueprintFeatureSelection','BlueprintCharacterClass','BlueprintArchetype','BlueprintActivatableAbility','BlueprintParametrizedFeature','BlueprintSpellList','BlueprintSpellbook','BlueprintSpellsTable'}:continue
        name,raw=native_raw[guid]
        if guid not in new_seen:
            new_seen.add(guid);records.append({'name':name.rsplit('/',1)[-1][:-4],'sha256':hashlib.sha256(raw).hexdigest().upper(),'blueprint':json.loads(raw)})
        upcoming.update(g for g in re.findall(r'!bp_([0-9a-f]{32})',raw.decode('utf-8-sig')) if g in native_raw)
    new_frontier=upcoming
found={r['blueprint']['AssetId'] for r in records}

if required-found: raise SystemExit('Missing required native dependencies: '+str(required-found))

assert link_ids=={r['blueprint']['AssetId'] for r in link_records}, 'Native linked spell missing from this game version'

pathlib.Path(sys.argv[2]).write_text(json.dumps({'requiredNative':records,'allNativeIds':all_ids,'linkedNative':link_records,'allNativeSelectionPools':selection_pools,'allNativeSpellLists':spell_lists,'nativeBlueprintTypes':native_types},ensure_ascii=False),encoding='utf-8')

print(f'Psychic extension: {len(records)} native dependencies, {len(all_ids)} native identities and {len(spell_lists)} native spell lists audited.')

