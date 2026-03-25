#pragma once

// Concurrent containers for fork-join parallel patterns.
//
// During the parallel phase, multiple threads insert data concurrently.
// The API deliberately never returns iterators, references, or pointers
// to internal data — these would be invalidated by rehash on the next
// concurrent insert.
//
// After the fork-join barrier, the owner calls consume()
// which atomically moves the internal data into a plain std container,
// transferring sole ownership to the caller. The caller can then iterate,
// read, etc. on the returned container with no thread-safety concerns.
//
// This pattern makes iterator-invalidation bugs structurally impossible:
//   - Concurrent phase: no iterators/references exposed.
//   - Sequential phase: plain std container, no concurrent modification.

#include <unordered_map>
#include <unordered_set>
#include <mutex>

namespace cswinrt
{
    // -----------------------------------------------------------------------
    // concurrent_unordered_map
    //
    // Concurrent: insert_or_assign, empty, size
    // Phase-transition: consume  (moves data out, resets container)
    // -----------------------------------------------------------------------
    template<typename K, typename V, typename H = std::hash<K>, typename E = std::equal_to<K>>
    class concurrent_unordered_map
    {
    public:
        concurrent_unordered_map() = default;

        void insert_or_assign(K const& key, V const& value)
        {
            std::lock_guard lock(m_mutex);
            m_data.insert_or_assign(key, value);
        }

        bool empty() const
        {
            std::lock_guard lock(m_mutex);
            return m_data.empty();
        }

        size_t size() const
        {
            std::lock_guard lock(m_mutex);
            return m_data.size();
        }

        // Atomically move all data out and reset. Returns a plain
        // std::unordered_map that the caller owns exclusively.
        std::unordered_map<K, V, H, E> consume()
        {
            std::lock_guard lock(m_mutex);
            return std::move(m_data);
        }

    private:
        std::unordered_map<K, V, H, E> m_data;
        mutable std::mutex m_mutex;
    };

    // -----------------------------------------------------------------------
    // concurrent_unordered_set
    //
    // Concurrent: insert, empty, size
    // Phase-transition: consume  (moves data out, resets container)
    // -----------------------------------------------------------------------
    template<typename T, typename H = std::hash<T>, typename E = std::equal_to<T>>
    class concurrent_unordered_set
    {
    public:
        concurrent_unordered_set() = default;

        void insert(T const& value)
        {
            std::lock_guard lock(m_mutex);
            m_data.insert(value);
        }

        void insert(T&& value)
        {
            std::lock_guard lock(m_mutex);
            m_data.insert(std::move(value));
        }

        bool empty() const
        {
            std::lock_guard lock(m_mutex);
            return m_data.empty();
        }

        size_t size() const
        {
            std::lock_guard lock(m_mutex);
            return m_data.size();
        }

        // Atomically move all data out and reset. Returns a plain
        // std::unordered_set that the caller owns exclusively.
        std::unordered_set<T, H, E> consume()
        {
            std::lock_guard lock(m_mutex);
            return std::move(m_data);
        }

    private:
        std::unordered_set<T, H, E> m_data;
        mutable std::mutex m_mutex;
    };
}
