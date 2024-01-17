/* Flight Plan
 * Copyright (C) 2024  schlosrat
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
namespace FlightPlan.Unity.Runtime
{
    /// <summary>
    /// This is an example Unity script that will also be compiled with your plugin. You can add scripts like
    /// this for example to create custom controls that you will then be able to use in both the editor and
    /// in the game.
    /// </summary>
    public class ExampleScript
    {
        /// <summary>
        /// Returns a greeting for the player based on the current time of day.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="isAfternoon">Whether it is currently afternoon.</param>
        /// <returns></returns>
        public static string GetGreeting(string playerName, bool isAfternoon)
        {
            return $"Good {(isAfternoon ? "afternoon" : "morning")}, {playerName}!";
        }
    }
}
