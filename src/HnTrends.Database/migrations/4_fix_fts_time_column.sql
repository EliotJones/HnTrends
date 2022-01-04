UPDATE search_target as st
SET `time` = (SELECT `time` FROM story WHERE id = st.id)
WHERE `time` is NULL;