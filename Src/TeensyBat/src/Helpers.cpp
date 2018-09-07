#include "Helpers.h"

#include "Config.h"

namespace Helpers
{
	bool LedsEnabled = true;
}

bool Helpers::CheckLedsEnabled()
{
#ifdef TB_DEBUG
	return true;
#endif
	if (LedsEnabled)
	{
		if (millis() < TB_AUTO_SWITCH_OFF_MS)
		{
			return true;
		}
		LedsEnabled = false;
	}
	return false;
}
