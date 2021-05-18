using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEL;
using SEL.API;

namespace SEL_Test
{
	[TestClass]
	public class RestrictionGeometryTypeManagerTests
	{
		[TestMethod]
		public void VerifyRestrictionSetup()
		{
			APIRestrictionTypeException[] apiData = {
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 0, allowed_ship_type_ids = new[] { 0, 1, 2 }},
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 1, allowed_ship_type_ids = new[] { 0, 1, 2 }},
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 2, allowed_ship_type_ids = new[] { 0, 1, 2 }},
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 3, allowed_ship_type_ids = new[] { 0, 1, 2 }}
			};

			RestrictionGeometryTypeManager manager = new RestrictionGeometryTypeManager();
			manager.ImportGeometryTypes(apiData);

			RestrictionGeometryType singleTargetType = new RestrictionGeometryType(new[] {0, 1, 2}, 1.0f, new []{ new GeometryType(12, 0) });
			RestrictionGeometryType singleType = manager.GetAllowedShipMask(12, new[] { 0 });
			Assert.IsTrue(singleType.GetAllowedShipTypeMask() == singleTargetType.GetAllowedShipTypeMask());

			RestrictionGeometryType compoundTargetType =
				new RestrictionGeometryType(new[] {0, 1, 2 }, 1.0f, new [] { new GeometryType(12, 0) });
			RestrictionGeometryType compoundType = manager.GetAllowedShipMask(12, new[] {0, 1, 2, 3});
			Assert.IsTrue(compoundType.GetAllowedShipTypeMask() == compoundTargetType.GetAllowedShipTypeMask());
		}

		[TestMethod]
		public void VerifyMultiplier()
		{
			APIRestrictionTypeException[] apiData = {
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 0, allowed_ship_type_ids = new[] { 0, 1, 2 }, cost_multipliers = new [] { 1.0f, 1.0f, 1.0f }},
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 1, allowed_ship_type_ids = new[] { 1, 2, 3 }, cost_multipliers = new [] { 2.0f, 2.0f, 2.0f }},
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 2, allowed_ship_type_ids = new[] { 2, 3, 4 }, cost_multipliers = new [] { 3.0f, 3.0f, 3.0f }},
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 3, allowed_ship_type_ids = new[] { 3, 4, 5 }, cost_multipliers = new [] { 4.0f, 4.0f, 4.0f }}
			};

			RestrictionGeometryTypeManager manager = new RestrictionGeometryTypeManager();
			manager.ImportGeometryTypes(apiData);

			RestrictionGeometryType singleType = manager.GetAllowedShipMask(12, new[] { 1 });
			Assert.IsTrue(singleType.GetShipCostMultiplier(1) == 2.0f);

			//Compound types should use the max of the multiplier.
			RestrictionGeometryType compoundType = manager.GetAllowedShipMask(12, new[] {0, 1, 2 });
			Assert.IsTrue(compoundType.GetShipCostMultiplier(2) == 3.0f);
		}

		[TestMethod]
		public void VerifyInvalidSetup()
		{
			APIRestrictionTypeException[] apiData = {
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 0, allowed_ship_type_ids = new[] { 0, 1, 2 }, cost_multipliers = new[] { 1.0f, 1.0f, 1.0f } },
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 1, allowed_ship_type_ids = new[] { 3, 4, 5 }, cost_multipliers = new[] { 1.0f, 1.0f, 1.0f } },
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 2, allowed_ship_type_ids = new[] { 6, 7, 8 }, cost_multipliers = new[] { 1.0f, 1.0f, 1.0f } },
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 3, allowed_ship_type_ids = new[] { 9,10,11 }, cost_multipliers = new[] { 1.0f, 1.0f, 1.0f } }
			};

			RestrictionGeometryTypeManager manager = new RestrictionGeometryTypeManager();
			manager.ImportGeometryTypes(apiData);

			RestrictionGeometryType type = manager.GetAllowedShipMask(123, new[] {123});
			Assert.IsTrue(type == RestrictionGeometryType.DisallowAll);
		}

		[TestMethod]
		public void VerifyMultiGroupMultiplier()
		{
			APIRestrictionTypeException[] apiData = {
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 0, allowed_ship_type_ids = new[] { 100, 101 }, cost_multipliers = new[] { 1.0f, 1.0f }},
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 0, allowed_ship_type_ids = new[] { 101 }, cost_multipliers = new[] { 2.0f }}
			};

			RestrictionGeometryTypeManager manager = new RestrictionGeometryTypeManager();
			manager.ImportGeometryTypes(apiData);

			RestrictionGeometryType singleType = manager.GetAllowedShipMask(12, new[] { 0 });
			Assert.IsTrue(singleType.GetShipCostMultiplier(101) == 2.0f);
		}

		[TestMethod]
		public void VerifyLayerComposition()
		{
			APIRestrictionTypeException[] apiData = {
				new APIRestrictionTypeException { layer_id = 12, layer_type_id = 0, allowed_ship_type_ids = new[] { 100, 101 }, cost_multipliers = new[] { 1.0f, 1.0f }},
				new APIRestrictionTypeException { layer_id = 13, layer_type_id = 0, allowed_ship_type_ids = new[] { 101 }, cost_multipliers = new[] { 2.0f }}
			};

			RestrictionGeometryTypeManager manager = new RestrictionGeometryTypeManager();
			manager.ImportGeometryTypes(apiData);

			RestrictionGeometryType singleType = manager.GetAllowedShipMask(12, new[] { 0 });
			Assert.IsTrue(singleType.CrossesGeometryTypes.Length == 1 && singleType.CrossesGeometryTypes[0].LayerId == 12);
			singleType = manager.GetAllowedShipMask(13, new[] { 0 });
			Assert.IsTrue(singleType.CrossesGeometryTypes.Length == 1 && singleType.CrossesGeometryTypes[0].LayerId == 13);
		}
	}
}
