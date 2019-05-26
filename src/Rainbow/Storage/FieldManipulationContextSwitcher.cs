using Sitecore.Common;

namespace Rainbow.Storage
{
	public class FieldManipulationContextSwitcher : Switcher<FieldManipulationContextSwitcher>
	{
		public IFieldValueManipulator Manipulator { get; }

		public FieldManipulationContextSwitcher(IFieldValueManipulator manipulator)
		{
			Manipulator = manipulator;
			Enter(this);
		}
	}
}
