#include <globjects/base/Changeable.h>

#include <cassert>

#include <globjects/base/ChangeListener.h>

namespace globjects
{

void Changeable::changed() const
{
	for (ChangeListener * listener: m_listeners)
	{
		listener->notifyChanged(this);
	}
}

void Changeable::registerListener(ChangeListener * listener)
{
    assert(listener != nullptr);

	m_listeners.insert(listener);
    listener->addSubject(this);
}

void Changeable::deregisterListener(ChangeListener * listener)
{
    assert(listener != nullptr);

    if (m_listeners.find(listener) == m_listeners.end())
        return;

	m_listeners.erase(listener);
    listener->removeSubject(this);
}

} // namespace globjects
