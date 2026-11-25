using UnityEditor;
using UnityEngine;
namespace CampusNavigation.Editor
{
 public static class RunTests
 {
  [MenuItem("Campus Navigation/4 - Run routing tests")]
  public static void Run(){NavGraphTests.Run();ReviewLoginTests.Run();ProjectBuilder.ValidateMap();Debug.Log("Routing and login checks passed. Hardware and AR phone tests are separate.");}
 }
}
