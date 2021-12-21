using FrooxEngine;
using BaseX;
namespace HeadlessTweaks
{
    public class ImpulsePassMethods
    {
        public static void NameSlot(World world, Slot parameterSlot)
        {
            Slot slot = Utilities.FetchParameter<Slot>(parameterSlot, "Target");
            string name = Utilities.FetchParameter<string>(parameterSlot, "Name");

            if (slot != null) slot.Name = name;
            return;
        }
        public static void SaveWorld(World world, Slot parameterSlot)
        {
            Userspace.SaveWorldAuto(world, SaveType.Overwrite, false);
            return;
        }
    }


    public class Utilities
    {
        [GenericTypes(GenericTypes.Group.NeosPrimitives, new System.Type[] { typeof(Slot), typeof(User) })]
        public static bool FetchParameter<T>(Slot slot, string variableName, out T value)
        {
            value = Coder<T>.Default;
            DynamicVariableSpace space = slot.FindSpace("HeadlessExec");
            if (space != null)
            {
                return space.TryReadValue<T>(variableName, out value);
            }
            return false;
        }
        public static T FetchParameter<T>(Slot slot, string variableName)
        {
            T value;
            if (FetchParameter<T>(slot, variableName, out value))
            {
                return value;
            }
            return Coder<T>.Default;
        }
    }

}
