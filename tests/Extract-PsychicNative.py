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

